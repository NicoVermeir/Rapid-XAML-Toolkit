﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text;
using RapidXamlToolkit.Resources;
using RapidXamlToolkit.XamlAnalysis.Actions;
using RapidXamlToolkit.XamlAnalysis.Tags;

namespace RapidXamlToolkit.XamlAnalysis.Processors
{
    public class RepeatButtonProcessor : XamlElementProcessor
    {
        public override void Process(int offset, string xamlElement, string linePadding, ITextSnapshot snapshot, List<IRapidXamlAdornmentTag> tags)
        {
            var (uidExists, uid) = this.GetOrGenerateUid(xamlElement, Attributes.Content);

            this.CheckForHardCodedAttribute(
                Elements.RepeatButton,
                Attributes.Content,
                AttributeType.Any,
                StringRes.Info_XamlAnalysisHardcodedStringRepeatButtonContentMessage,
                xamlElement,
                snapshot,
                offset,
                uidExists,
                uid,
                Guid.Empty,
                tags);
        }
    }
}
