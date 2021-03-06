﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.VisualStudio.Text;
using RapidXamlToolkit.Resources;
using RapidXamlToolkit.XamlAnalysis.Actions;

namespace RapidXamlToolkit.XamlAnalysis.Tags
{
    public class CheckBoxCheckedAndUncheckedEventsTag : RapidXamlWarningTag
    {
        // https://docs.microsoft.com/en-us/windows/uwp/design/controls-and-patterns/checkbox#handle-click-and-checked-events
        public CheckBoxCheckedAndUncheckedEventsTag(Span span, ITextSnapshot snapshot, int line, int column)
            : base(span, snapshot, "RXT401", line, column)
        {
            this.SuggestedAction = null;
            this.ToolTip = StringRes.Info_XamlAnalysisCheckBoxCheckedAndUncheckedEventsToolTip;
            this.Description = StringRes.Info_XamlAnalysisCheckBoxCheckedAndUncheckedEventsDescription;
            this.ExtendedMessage = StringRes.Info_XamlAnalysisCheckBoxCheckedAndUncheckedEventsExtendedMessage;
        }
    }
}
