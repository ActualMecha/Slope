using HostMgd.EditorInput;
using Slope.Model;
using System;
using Teigha.DatabaseServices;

namespace Slope
{
    public class Selector : IDisposable
    {
        private enum State
        {
            BeginTop, ContinueTop, BeginBottom, ContinueBottom, Finished
        }

        private State CurrentState = State.BeginTop;
        public PolyUniversalLine Top = new PolyUniversalLine();
        public PolyUniversalLine Bottom = new PolyUniversalLine();
        private readonly Editor Editor;
        private readonly TransactionManager TransactionManager;


        public Selector(Editor editor, TransactionManager transactionManager)
        {
            Editor = editor;
            TransactionManager = transactionManager;
        }

        public void SelectLines()
        {
            while (InterpretInput(SelectLine(Prompt)))
                ;
        }
        private UniversalLine SelectLine(string prompt)
        {
            var options = new PromptEntityOptions(prompt);
            options.AddAllowedClass(typeof(Polyline), false);
            options.AddAllowedClass(typeof(Line), false);
            var result = Editor.GetEntity(options);
            if (result.Status != PromptStatus.OK)
                return null;

            var curve = result.ObjectId.GetObject(OpenMode.ForWrite) as Curve;
            return new SimpleUniversalLine(result.ObjectId.GetObject(OpenMode.ForWrite) as Curve);
        }

        private string Prompt
        {
            get
            {
                switch (CurrentState)
                {
                    case State.BeginTop: return "Select top";
                    case State.ContinueTop: return "Continue top or select bottom";
                    case State.BeginBottom: return "Begin bottom";
                    case State.ContinueBottom: return "Continue bottom";
                    default: throw new ArgumentOutOfRangeException();
                }
            }
        }

        private bool InterpretInput(UniversalLine line)
        {
            switch (CurrentState)
            {
                case State.BeginTop: return BeginTop(line);
                case State.ContinueTop: return ContinueTop(line);
                case State.BeginBottom: return BeginBottom(line);
                case State.ContinueBottom: return ContinueBottom(line);
            }
            return false;
        }

        private bool BeginTop(UniversalLine line)
        {
            if (line == null) throw new UserCancelledException();
            return ContinueTop(line);
        }

        private bool ContinueTop(UniversalLine line)
        {
            if (line == null)
            {
                CurrentState = State.BeginBottom;
                return true;
            }
            if (!Top.TryAddSegment(line))
            {
                line.Dispose();
                return true;
            }
            CurrentState = State.ContinueTop;
            VisuallySelect(Top);
            return true;
        }

        private bool BeginBottom(UniversalLine line)
        {
            if (line == null) throw new UserCancelledException();
            return ContinueBottom(line);
        }

        private bool ContinueBottom(UniversalLine line)
        {
            if (line == null)
            {
                CurrentState = State.Finished;
                return false;
            }
            if (!Bottom.TryAddSegment(line))
            {
                line.Dispose();
                return true;
            }
            CurrentState = State.ContinueBottom;
            VisuallySelect(Bottom);
            return true;
        }

        private void VisuallySelect(UniversalLine line)
        {
            line.Hightlight();
        }

        public void Dispose()
        {
            Top.Unhighlight();
            Bottom.Unhighlight();
            Top.Dispose();
            Bottom.Dispose();
        }
    }
}
