using HostMgd.ApplicationServices;
using HostMgd.EditorInput;
using Slope.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using Teigha.DatabaseServices;
using Teigha.Geometry;
using Teigha.Runtime;

namespace Slope
{
    public class Slope
    {
        [CommandMethod("Slope")]
        public static void Run()
        {
            var intersectionCandidates = new LinkedList<Line>();
            var addedLines = new List<Line>();
            try
            {
                using (var editor = Application.DocumentManager.MdiActiveDocument.Editor)
                using (var database = Application.DocumentManager.MdiActiveDocument.Database)
                using (var transactionManager = database.TransactionManager)
                using (var transaction = transactionManager.StartTransaction())
                using (var selector = new Selector(editor, transactionManager))
                {
                    selector.SelectLines();
                    selector.Top.ChopIntersections();
                    selector.Bottom.ChopIntersections();
                    using (var blockTable = (BlockTable)transaction.GetObject(database.BlockTableId, OpenMode.ForRead, false))
                    using (var currentLayout = (BlockTableRecord)transaction.GetObject(database.CurrentSpaceId, OpenMode.ForWrite, false))
                    {
                        var top = selector.Top;
                        var bottom = selector.Bottom;
                        var step = SelectStep("Step");
                        PlanLines(intersectionCandidates, addedLines, top, bottom, step);

                        using (var layer = FindOrCreateLayer("!!Штриховка откосов", transaction, database))
                        {
                            foreach (var line in addedLines)
                            {
                                line.LayerId = layer.Id;
                                AddLine(currentLayout, transaction, line);
                            }
                        }
                        transaction.Commit();
                    }
                }
            }
            catch (UserCancelledException)
            {
            }
            finally
            {
                foreach (var line in addedLines)
                    line.Dispose();
            }
        }

        private static void AddLine(BlockTableRecord block, Transaction transaction, Line line)
        {
            block.AppendEntity(line);
            transaction.AddNewlyCreatedDBObject(line, true);
        }

        private static double SelectStep(string prompt)
        {
            var editor = Application.DocumentManager.MdiActiveDocument.Editor;
            var options = new PromptDistanceOptions(prompt)
            {
                AllowNegative = false,
                AllowZero = false
            };
            var result = editor.GetDistance(options);
            if (result.Status != PromptStatus.OK)
                throw new UserCancelledException();
            return result.Value;
        }

        private static LayerTableRecord FindOrCreateLayer(string name, Transaction transaction, Database database)
        {
            using (var layerTable = (LayerTable)transaction.GetObject(database.LayerTableId, OpenMode.ForWrite, false))
            {
                LayerTableRecord record;
                if (layerTable.Has(name))
                {

                    record = (LayerTableRecord)transaction.GetObject(layerTable[name], OpenMode.ForWrite);
                }
                else
                {
                    record = new LayerTableRecord
                    {
                        Name = name
                    };

                    layerTable.Add(record);
                    transaction.AddNewlyCreatedDBObject(record, true);
                }
                return record;
            }
        }

        private static void ChopEnds(List<Line> addedLines)
        {
            var prevChop = true;
            var end = addedLines.Count - 1;
            for (var i = 0; i < addedLines.Count; ++i)
            {
                var chop = false;
                for (var newEnd = end; newEnd >= addedLines.Count - i - 1 && !chop; --newEnd)
                {
                    chop = ChopBothLines(addedLines[i], addedLines[newEnd]);
                    if (chop)
                        end = newEnd - 1;
                }
                if (!chop && !prevChop)
                    break;
                prevChop = chop;
            }
        }

        private static void PlanLines(LinkedList<Line> intersectionCandidates,
            List<Line> addedLines,
            UniversalLine top,
            UniversalLine bottom,
            double step)
        {
            var side = top.GetSide(bottom);
            var halve = false;
            foreach (var topLine in top.Segments)
                for (var offset = 0.0; offset < topLine.Length; offset += step)
                {
                    var normalOffset = offset / topLine.Length;
                    var pointOnLine = topLine.EvaluatePoint(normalOffset);
                    var tangent = top.GetFirstDerivative(pointOnLine);
                    var normal = tangent.RotateBy((int)side * Math.PI / 2.0, new Vector3d(0, 0, 1));
                    var line = new Line(pointOnLine.Point, pointOnLine.Point.Add(normal));
                    var intersects = bottom.ChopLine(line, true);
                    if (intersects)
                    {
                        if (halve)
                            line.EndPoint = new Point3d((line.StartPoint.X + line.EndPoint.X) / 2, (line.StartPoint.Y + line.EndPoint.Y) / 2, 0);

                        halve = !halve;
                        addedLines.Add(line);
                        if (intersectionCandidates.Count == 0)
                            intersectionCandidates.AddLast(line);
                        else if (ChopBothLines(line, intersectionCandidates.Last.Value))
                            intersectionCandidates.RemoveLast();
                        else if (intersectionCandidates.Count > 1 && ChopBothLines(line, intersectionCandidates.Last.Previous.Value))
                        {
                            intersectionCandidates.RemoveLast();
                            intersectionCandidates.RemoveLast();
                        }
                        else if (intersectionCandidates.Count > 2 && ChopBothLines(line, intersectionCandidates.Last.Previous.Previous.Value))
                        {
                            intersectionCandidates.RemoveLast();
                            intersectionCandidates.RemoveLast();
                            intersectionCandidates.RemoveLast();
                        }
                        else
                            intersectionCandidates.AddLast(line);
                    }
                    else
                        line.Dispose();
                }
            ChopEnds(addedLines);
        }

        private static bool ChopBothLines(Line line1, Line line2)
        {
            if (ChopLine(line1, line2, false))
            {
                line2.EndPoint = line1.EndPoint;
                return true;
            }
            return false;
        }

        private static bool ChopLine(Line line, Entity entity, bool extend)
        {
            var forward = line.StartPoint.GetVectorTo(line.EndPoint);
            using (var intersection = new Point3dCollection())
            {
                line.IntersectWith(entity, extend ? Intersect.ExtendThis : Intersect.OnBothOperands, intersection, IntPtr.Zero, IntPtr.Zero);
                Point3d? first = null;
                var shortestLength = double.PositiveInfinity;
                for (var i = 0; i < intersection.Count; ++i)
                {
                    var point = intersection[i];
                    if (line.StartPoint.GetVectorTo(point).DotProduct(forward) < 0)
                        continue;
                    var length = line.StartPoint.GetVectorTo(point).LengthSqrd;
                    if (length < shortestLength)
                    {
                        shortestLength = length;
                        first = point;
                    }
                }
                if (first.HasValue)
                    line.EndPoint = first.Value;
                return first.HasValue;
            }
        }
    }
}