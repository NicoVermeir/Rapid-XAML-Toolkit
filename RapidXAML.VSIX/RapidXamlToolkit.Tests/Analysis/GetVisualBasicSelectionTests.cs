﻿// <copyright file="GetVisualBasicSelectionTests.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RapidXamlToolkit.Tests.Analysis
{
    [TestClass]
    public class GetVisualBasicSelectionTests : VisualBasicTestsBase
    {
        [TestMethod]
        public void GetSelectionOfMultipleProperties()
        {
            var code = @"
Namespace tests
    Public Class SomeClass
        private _property8 As String    *

        Public Property Property1 As String
        Public ReadOnly Property Property2 As String
        Private ReadOnly Property Property3 As String
        Private Property Property4 As String
        Public Property Property5 As Integer
        Public Property Property6 As List(Of String)
        Friend Property Property7 As String
        Public Property Property8 As String
            Get
                Return _property8
            End Get
            Set
                _property8 = value
            End Set
        End Property*
    End Class
End Namespace";

            var expectedOutput = "<TextBox Text=\"{x:Bind Property1, Mode=TwoWay}\" />"
         + Environment.NewLine + "<TextBlock Text=\"Property2\" />"
         + Environment.NewLine + "<TextBlock Text=\"Property3\" />"
         + Environment.NewLine + "<TextBox Text=\"{x:Bind Property4, Mode=TwoWay}\" />"
         + Environment.NewLine + "<Slider Minimum=\"0\" Maximum=\"100\" x:Name=\"Property5\" Value=\"{x:Bind Property5, Mode=TwoWay}\" />"
         + Environment.NewLine + "<ItemsControl ItemsSource=\"{x:Bind Property6}\"></ItemsControl>"
         + Environment.NewLine + "<TextBox Text=\"{x:Bind Property7, Mode=TwoWay}\" />"
         + Environment.NewLine + "<TextBox Text=\"{x:Bind Property8, Mode=TwoWay}\" />";

            var expected = new AnalyzerOutput
            {
                Name = "Property1, Property2 and 6 other properties",
                Output = expectedOutput,
                OutputType = AnalyzerOutputType.Selection,
            };

            this.SelectionBetweenStarsShouldProduceExpected(code, expected);
        }

        [TestMethod]
        public void GetSelectionWithinSingleProperties()
        {
            var code = @"
Namespace tests
    Public Class SomeClass
        Public Read*Only Property Property2 As S*tring
    End Class
End Namespace";

            this.SinglePropertySelectionTest(code);
        }

        [TestMethod]
        public void GetSelectionOverStartOfSingleProperties()
        {
            var code = @"
Namespace tests
    Public Class SomeCl*ass
        Public ReadOnly Pro*perty Property2 As String
    End Class
End Namespace";

            this.SinglePropertySelectionTest(code);
        }

        [TestMethod]
        public void GetSelectionOverEndOfSingleProperties()
        {
            var code = @"
Namespace tests
    Public Class SomeClass
        Public ReadOnly Proper*ty Property2 As String
    End Class*
End Namespace";

            this.SinglePropertySelectionTest(code);
        }

        [TestMethod]
        public void GetSelectionNothingFoundOverClassDeclaration()
        {
            var code = @"
Namespace tests
    *Public Class SomeClass*
        Public ReadOnly Property Property2 As String
    End Class
End Namespace";

            this.NoPropertiesFoundInSelectionTest(code);
        }

        [TestMethod]
        public void GetSelectionNothingFoundOverUsingStatements()
        {
            var code = @"
us*ing System;
using Windows.Xaml;
*
Namespace tests
    Public Class SomeClass
        Public ReadOnly Property Property2 As String
    End Class
End Namespace";

            this.NoPropertiesFoundInSelectionTest(code);
        }

        [TestMethod]
        public void GetSelectionNothingFoundOverField()
        {
            var code = @"
Namespace tests
    Public Class SomeClass
 *       Private _someField As Integer = 3*

        Public ReadOnly Property Property2 As String
    End Class
End Namespace";

            this.NoPropertiesFoundInSelectionTest(code);
        }

        [TestMethod]
        public void GetSelectionNothingFoundOverMethod()
        {
            var code = @"
Namespace tests
    Public Class SomeClass
        Public ReadOnly Property Property2 As String

      *  Public Function IsSpecial(someValue As String) As Boolean
            Return True
        End Function*
    End Class
End Namespace";

            this.NoPropertiesFoundInSelectionTest(code);
        }

        [TestMethod]
        public void GetSelectionNothingFoundOverConstructor()
        {
            var code = @"
Namespace tests
    Public Class SomeClass
        Public Property Property2 As String

      * Public Sub New()
            Property2 = ""set""
        End Sub*
    End Class
End Namespace";

            this.NoPropertiesFoundInSelectionTest(code);
        }

        [TestMethod]
        public void CanHandleMultipleNumberReplacementsWithinSelection()
        {
            var gridProfile = new Profile
            {
                Name = "GridTestProfile",
                ClassGrouping = "Grid",
                DefaultOutput = "<TextBlock Text=\"FALLBACK_{NAME}\" />",
                Mappings = new List<Mapping>
                {
                    new Mapping
                    {
                        Type = "string",
                        NameContains = "",
                        Output = "<TextBlock Text=\"{NAME}\" Grid.Row=\"{X}\" />",
                        IfReadOnly = false,
                    },
                },
            };

            var code = @"
Namespace tests
    Public Class Class100
        Public Property Property1 As String
       * Public Property Property2 As String
        Public Property Property3 As String *
        Public Property Property4 As String
    End Class
End Namespace";

            var expectedOutput = "<TextBlock Text=\"Property2\" Grid.Row=\"1\" />"
         + Environment.NewLine + "<TextBlock Text=\"Property3\" Grid.Row=\"2\" />";

            var expected = new AnalyzerOutput
            {
                Name = "Property2 and Property3",
                Output = expectedOutput,
                OutputType = AnalyzerOutputType.Selection,
            };

            this.SelectionBetweenStarsShouldProduceExpected(code, expected, gridProfile);
        }

        [TestMethod]
        public void GetSelectionOfCustomPropertyWithSubProperties()
        {
            var recurseProfile = new Profile
            {
                Name = "GridTestProfile",
                ClassGrouping = "Grid",
                DefaultOutput = "<TextBlock Text=\"FB_{NAME}\" />",
                Mappings = new List<Mapping>
                {
                    new Mapping
                    {
                        Type = "Order",
                        NameContains = "",
                        Output = "<StackPanel>{SUBPROPERTIES}</StackPanel>",
                        IfReadOnly = false,
                    },
                },
            };

            var code = @"
Namespace tests
    Public Class Class1
        *Public Property LastOrder As Order*
    End Class

    Class Order
        Public Property OrderId As Integer
        Public ReadOnly Property OrderPlacedDateTime As DateTime
        Public Property OrderDescription As String
    End Class
End Namespace";

            // This includes the readonly property as not yet filtering out
            // All types treated as fallback
            var expectedOutput = "<StackPanel>"
         + Environment.NewLine + "<TextBlock Text=\"FB_OrderId\" />"
         + Environment.NewLine + "<TextBlock Text=\"FB_OrderPlacedDateTime\" />"
         + Environment.NewLine + "<TextBlock Text=\"FB_OrderDescription\" />"
         + Environment.NewLine + "</StackPanel>";

            var expected = new AnalyzerOutput
            {
                Name = "LastOrder",
                Output = expectedOutput,
                OutputType = AnalyzerOutputType.Selection,
            };

            this.SelectionBetweenStarsShouldProduceExpected(code, expected, recurseProfile);
        }

        private void SinglePropertySelectionTest(string code)
        {
            var expectedOutput = "<TextBlock Text=\"Property2\" />";

            var expected = new AnalyzerOutput
            {
                Name = "Property2",
                Output = expectedOutput,
                OutputType = AnalyzerOutputType.Selection,
            };

            this.SelectionBetweenStarsShouldProduceExpected(code, expected);
        }

        private void NoPropertiesFoundInSelectionTest(string code)
        {
            var expected = AnalyzerOutput.Empty;

            this.SelectionBetweenStarsShouldProduceExpected(code, expected);
        }
    }
}