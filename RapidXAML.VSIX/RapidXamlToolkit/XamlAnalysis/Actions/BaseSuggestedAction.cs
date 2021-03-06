﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace RapidXamlToolkit.XamlAnalysis.Actions
{
    public abstract class BaseSuggestedAction : ISuggestedAction
    {
        protected BaseSuggestedAction(string file)
        {
            this.File = file;
        }

        public string DisplayText { get; protected set; }

        public virtual bool IsEnabled { get; } = true;

        public virtual bool HasActionSets
        {
            get { return false; }
        }

        public virtual bool HasPreview
        {
            get { return false; }
        }

        public string IconAutomationText
        {
            get { return null; }
        }

        public virtual ImageMoniker IconMoniker
        {
            get { return default(ImageMoniker); }
        }

        public string InputGestureText
        {
            get { return null; }
        }

        protected string File { get; }

        protected ITextView View { get; set; }

        public virtual void Dispose()
        {
            // nothing to dispose
        }

        public Task<IEnumerable<SuggestedActionSet>> GetActionSetsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IEnumerable<SuggestedActionSet>>(null);
        }

        public virtual Task<object> GetPreviewAsync(CancellationToken cancellationToken)
        {
            return null;
        }

        public void Invoke(CancellationToken cancellationToken)
        {
            try
            {
                ProjectHelpers.Dte2.UndoContext.Open(this.DisplayText);
                this.Execute(cancellationToken);
            }
            catch (Exception exc)
            {
                RapidXamlPackage.Logger?.RecordException(exc);
            }
            finally
            {
                ProjectHelpers.Dte2.UndoContext.Close();
            }
        }

        public abstract void Execute(CancellationToken cancellationToken);

        public bool TryGetTelemetryId(out Guid telemetryId)
        {
            telemetryId = Guid.Empty;
            return false;
        }

        protected static IEnumerable<ITextSnapshotLine> GetSelectedLines(SnapshotSpan span, out SnapshotSpan wholeSpan)
        {
            var startLine = span.Start.GetContainingLine();
            var endLine = span.End.GetContainingLine();

            wholeSpan = new SnapshotSpan(startLine.Start, endLine.End);
            return span.Snapshot.Lines.Where(l => l.LineNumber >= startLine.LineNumber && l.LineNumber <= endLine.LineNumber);
        }

        // Call this after having made the change if need to force reevaluation of actions
        private void RaiseBufferChange()
        {
            // Adding and deleting a char in order to force taggers re-evaluation
            string text = " ";
            this.View?.TextBuffer.Insert(0, text);
            this.View?.TextBuffer.Delete(new Span(0, text.Length));
        }
    }
}
