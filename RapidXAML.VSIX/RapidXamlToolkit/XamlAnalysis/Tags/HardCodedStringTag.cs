﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Microsoft.VisualStudio.Text;
using RapidXamlToolkit.Resources;
using RapidXamlToolkit.XamlAnalysis.Actions;

namespace RapidXamlToolkit.XamlAnalysis.Tags
{
    public class HardCodedStringTag : RapidXamlWarningTag
    {
        public HardCodedStringTag(Span span, ITextSnapshot snapshot, int line, int column, string elementName, string attributeName)
            : base(span, snapshot, "RXT200", line, column)
        {
            this.SuggestedAction = typeof(HardCodedStringAction);
            this.ToolTip = StringRes.Info_XamlAnalysisHardcodedStringTooltip;
            this.ExtendedMessage = StringRes.Info_XamlAnalysisHardcodedStringExtendedMessage;
            this.ElementName = elementName;
            this.AttributeName = attributeName;
        }

        public AttributeType AttributeType { get; set; }

        public string Value { get; set; }

        public bool UidExists { get; set; }

        public string UidValue { get; set; }

        public string ElementName { get; }

        public string AttributeName { get; }

        public Guid ElementGuid { get; set; }
    }
}
