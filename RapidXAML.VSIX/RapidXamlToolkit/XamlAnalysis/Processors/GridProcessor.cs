﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text;
using RapidXamlToolkit.Resources;
using RapidXamlToolkit.XamlAnalysis.Tags;

namespace RapidXamlToolkit.XamlAnalysis.Processors
{
    public class GridProcessor : XamlElementProcessor
    {
        public override void Process(int offset, string xamlElement, string linePadding, ITextSnapshot snapshot, List<IRapidXamlAdornmentTag> tags)
        {
            const string gridOpenSpace = "<Grid ";
            const string gridOpenComplete = "<Grid>";

            var endOfOpening = xamlElement.IndexOf(">", StringComparison.Ordinal) + 1;
            var firstNestedGrid = xamlElement.FirstIndexOf(gridOpenSpace, gridOpenComplete);

            var rowDefPos = xamlElement.IndexOf("<Grid.RowDefinitions", StringComparison.Ordinal);
            var colDefPos = xamlElement.IndexOf("<Grid.ColumnDefinitions", StringComparison.Ordinal);

            var gridIsSelfClosing = XamlElementProcessor.IsSelfClosing(xamlElement);

            var hasRowDef = false;
            if (rowDefPos > 0)
            {
                hasRowDef = firstNestedGrid <= 0 || rowDefPos < firstNestedGrid;
            }

            var hasColDef = false;
            if (colDefPos > 0)
            {
                hasColDef = firstNestedGrid <= 0 || colDefPos < firstNestedGrid;
            }

            var leftPad = linePadding.Contains("\t") ? linePadding + "\t" : linePadding + "    ";

            var rowDefsClosingPos = -1;

            if (!hasRowDef)
            {
                var tag = new AddRowDefinitionsTag(new Span(offset, endOfOpening), snapshot)
                {
                    InsertPosition = offset + endOfOpening,
                    LeftPad = leftPad,
                    GridNeedsExpanding = gridIsSelfClosing,
                };
                tags.Add(tag);

                rowDefsClosingPos = xamlElement.IndexOf(">", StringComparison.Ordinal);
            }
            else
            {
                rowDefsClosingPos = xamlElement.IndexOf("</Grid.RowDefinitions", StringComparison.Ordinal);
            }

            var colDefsClosingPos = -1;

            if (!hasColDef)
            {
                var tag = new AddColumnDefinitionsTag(new Span(offset, endOfOpening), snapshot)
                {
                    InsertPosition = offset + endOfOpening,
                    LeftPad = leftPad,
                    GridNeedsExpanding = gridIsSelfClosing,
                };
                tags.Add(tag);

                colDefsClosingPos = xamlElement.IndexOf(">", StringComparison.Ordinal);
            }
            else
            {
                colDefsClosingPos = xamlElement.IndexOf("</Grid.ColumnDefinitions", StringComparison.Ordinal);
            }

            if (!hasRowDef && !hasColDef)
            {
                var tag = new AddRowAndColumnDefinitionsTag(new Span(offset, endOfOpening), snapshot)
                {
                    InsertPosition = offset + endOfOpening,
                    LeftPad = leftPad,
                    GridNeedsExpanding = gridIsSelfClosing,
                };
                tags.Add(tag);
            }

            const string rowDefStart = "<RowDefinition";

            var rowDefsCount = 0;

            var toAdd = new List<InsertRowDefinitionTag>();

            var rowDefIndex = xamlElement.IndexOf(rowDefStart, StringComparison.Ordinal);

            while (rowDefIndex >= 0)
            {
                var endPos = xamlElement.IndexOf('>', rowDefIndex);

                var tag = new InsertRowDefinitionTag(new Span(offset + rowDefIndex, endPos - rowDefIndex + 1), snapshot)
                {
                    RowId = rowDefsCount,
                    GridStartPos = offset,
                    GridLength = xamlElement.Length,
                    XamlTag = xamlElement.Substring(rowDefIndex, endPos - rowDefIndex + 1),
                    InsertPoint = offset + rowDefIndex,
                };

                rowDefsCount += 1;

                toAdd.Add(tag);

                rowDefIndex = xamlElement.IndexOf(rowDefStart, endPos, StringComparison.Ordinal);
            }

            foreach (var tag in toAdd)
            {
                tag.RowCount = rowDefsCount;
                tags.Add(tag);
            }

            const string colDef = "<ColumnDefinition";

            var colDefsCount = 0;

            var colDefIndex = xamlElement.IndexOf(colDef, StringComparison.Ordinal);

            while (colDefIndex > -1)
            {
                colDefsCount += 1;

                colDefIndex = xamlElement.IndexOf(colDef, colDefIndex + 1, StringComparison.Ordinal);
            }

            const string rowDefUse = "Grid.Row=\"";
            const string colDefUse = "Grid.Column=\"";

            int highestAssignedRow = -1;
            int highestAssignedCol = -1;

            var undefinedTags = new List<MissingDefinitionTag>();

            var nextDefUseIndex = xamlElement.FirstIndexOf(rowDefUse, colDefUse);
            var defUseOffset = 0;

            while (nextDefUseIndex > 0)
            {
                defUseOffset += nextDefUseIndex;

                var line = snapshot.GetLineFromPosition(offset + defUseOffset);
                var col = offset + defUseOffset - line.Start.Position;

                if (nextDefUseIndex > endOfOpening)
                {
                    // Get assigned value
                    if (xamlElement.Substring(defUseOffset).StartsWith(rowDefUse))
                    {
                        var valueStartPos = defUseOffset + rowDefUse.Length;
                        var closePos = xamlElement.IndexOf("\"", valueStartPos, StringComparison.Ordinal);

                        var assignedStr = xamlElement.Substring(valueStartPos, closePos - valueStartPos);

                        if (int.TryParse(assignedStr, out int assignedInt))
                        {
                            if (assignedInt > 0 && assignedInt >= rowDefsCount)
                            {
                                undefinedTags.Add(new MissingRowDefinitionTag(
                                    new Span(offset + defUseOffset, closePos - defUseOffset + 1),
                                    snapshot,
                                    line.LineNumber,
                                    col)
                                {
                                    AssignedInt = assignedInt,
                                    Description = StringRes.Info_XamlAnalysisMissingRowDefinitionDescription.WithParams(assignedInt),
                                    ExistingDefsCount = rowDefsCount,
                                    HasSomeDefinitions = hasRowDef,
                                    InsertPosition = offset + rowDefsClosingPos,
                                    LeftPad = leftPad,
                                });
                            }

                            if (assignedInt > highestAssignedRow)
                            {
                                highestAssignedRow = assignedInt;
                            }
                        }
                    }
                    else if (xamlElement.Substring(defUseOffset).StartsWith(colDefUse))
                    {
                        var valueStartPos = defUseOffset + colDefUse.Length;
                        var closePos = xamlElement.IndexOf("\"", valueStartPos, StringComparison.Ordinal);

                        var assignedStr = xamlElement.Substring(valueStartPos, closePos - valueStartPos);

                        if (int.TryParse(assignedStr, out int assignedInt))
                        {
                            if (assignedInt > 0 && assignedInt >= colDefsCount)
                            {
                                undefinedTags.Add(new MissingColumnDefinitionTag(
                                    new Span(offset + defUseOffset, closePos - defUseOffset + 1),
                                    snapshot,
                                    line.LineNumber,
                                    col)
                                {
                                    AssignedInt = assignedInt,
                                    Description = StringRes.Info_XamlAnalysisMissingColumnDefinitionDescription.WithParams(assignedInt),
                                    ExistingDefsCount = colDefsCount,
                                    HasSomeDefinitions = hasColDef,
                                    InsertPosition = offset + colDefsClosingPos,
                                    LeftPad = leftPad,
                                });
                            }

                            if (assignedInt > highestAssignedCol)
                            {
                                highestAssignedCol = assignedInt;
                            }
                        }
                    }
                }

                nextDefUseIndex = xamlElement.Substring(defUseOffset + 1).FirstIndexOf(colDefUse, rowDefUse) + 1;
            }

            foreach (var undefinedTag in undefinedTags)
            {
                undefinedTag.TotalDefsRequired = undefinedTag is MissingRowDefinitionTag ? highestAssignedRow
                                                                                         : highestAssignedCol;
                tags.Add(undefinedTag);
            }

            const string rowSpanUse = "Grid.RowSpan=\"";
            const string colSpanUse = "Grid.ColumnSpan=\"";

            var nextSpanUseIndex = xamlElement.FirstIndexOf(rowSpanUse, colSpanUse);
            var spanUseOffset = 0;

            while (nextSpanUseIndex > 0)
            {
                spanUseOffset += nextSpanUseIndex;

                var line = snapshot.GetLineFromPosition(offset + spanUseOffset);
                var col = offset + spanUseOffset - line.Start.Position;

                if (nextSpanUseIndex > endOfOpening)
                {
                    if (xamlElement.Substring(spanUseOffset).StartsWith(rowSpanUse))
                    {
                        var valueStartPos = spanUseOffset + rowSpanUse.Length;
                        var closePos = xamlElement.IndexOf("\"", valueStartPos, StringComparison.Ordinal);

                        var assignedStr = xamlElement.Substring(valueStartPos, closePos - valueStartPos);

                        if (int.TryParse(assignedStr, out int assignedInt))
                        {
                            var element = XamlElementProcessor.GetSubElementAtPosition(xamlElement, spanUseOffset);

                            var row = 0;
                            if (this.TryGetAttribute(element, "Grid.Row", AttributeType.InlineOrElement, out _, out _, out _, out string rowStr))
                            {
                                row = int.Parse(rowStr);
                            }

                            if (assignedInt > 1 && assignedInt - 1 + row >= rowDefsCount)
                            {
                                tags.Add(new RowSpanOverflowTag(
                                    new Span(offset + spanUseOffset, closePos - spanUseOffset + 1),
                                    snapshot,
                                    line.LineNumber,
                                    col)
                                {
                                    TotalDefsRequired = assignedInt + row - 1,
                                    Description = StringRes.Info_XamlAnalysisRowSpanOverflowDescription,
                                    ExistingDefsCount = rowDefsCount,
                                    HasSomeDefinitions = hasRowDef,
                                    InsertPosition = offset + rowDefsClosingPos,
                                    LeftPad = leftPad,
                                });
                            }
                        }
                    }
                    else if (xamlElement.Substring(spanUseOffset).StartsWith(colSpanUse))
                    {
                        var valueStartPos = spanUseOffset + colSpanUse.Length;
                        var closePos = xamlElement.IndexOf("\"", valueStartPos, StringComparison.Ordinal);

                        var assignedStr = xamlElement.Substring(valueStartPos, closePos - valueStartPos);

                        if (int.TryParse(assignedStr, out int assignedInt))
                        {
                            var element = XamlElementProcessor.GetSubElementAtPosition(xamlElement, spanUseOffset);

                            var gridCol = 0;
                            if (this.TryGetAttribute(element, "Grid.Column", AttributeType.InlineOrElement, out _, out _, out _, out string colStr))
                            {
                                gridCol = int.Parse(colStr);
                            }

                            if (assignedInt > 1 && assignedInt - 1 + gridCol >= colDefsCount)
                            {
                                tags.Add(new ColumnSpanOverflowTag(
                                    new Span(offset + spanUseOffset, closePos - spanUseOffset + 1),
                                    snapshot,
                                    line.LineNumber,
                                    gridCol)
                                {
                                    TotalDefsRequired = assignedInt - 1 + gridCol,
                                    Description = StringRes.Info_XamlAnalysisColumnSpanOverflowDescription,
                                    ExistingDefsCount = colDefsCount,
                                    HasSomeDefinitions = hasColDef,
                                    InsertPosition = offset + colDefsClosingPos,
                                    LeftPad = leftPad,
                                });
                            }
                        }
                    }
                }

                nextSpanUseIndex = xamlElement.Substring(spanUseOffset + 1).FirstIndexOf(colSpanUse, rowSpanUse) + 1;
            }
        }
    }
}
