#region License
// Copyright (c) 2007 James Newton-King
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
#endregion

#if !(NET20 || NET35 || NET40 || PORTABLE40)

using System;
using System.Globalization;
#if !PORTABLE || NETSTANDARD1_1
using System.Numerics;
#endif
using System.Text;
#if DNXCORE50
using Xunit;
using Test = Xunit.FactAttribute;
using Assert = Newtonsoft.Json.Tests.XUnitAssert;
#else
using NUnit.Framework;
#endif
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json.Tests.TestObjects.JsonTextReaderTests;

namespace Newtonsoft.Json.Tests.JsonTextReaderTests
{
    [TestFixture]
#if !DNXCORE50
    [Category("JsonTextReaderTests")]
#endif
    public class ExceptionHandlingAsyncTests : TestFixtureBase
    {
        [Test]
        public async Task UnexpectedEndAfterReadingNAsync()
        {
            JsonTextReader reader = new JsonTextReader(new StringReader("n"));
            await ExceptionAssert.ThrowsAsync<JsonReaderException>(async () => await reader.ReadAsync().ConfigureAwait(true), "Unexpected end when reading JSON. Path '', line 1, position 1.").ConfigureAwait(true);
        }

        [Test]
        public async Task UnexpectedEndAfterReadingNuAsync()
        {
            JsonTextReader reader = new JsonTextReader(new StringReader("nu"));
            await ExceptionAssert.ThrowsAsync<JsonReaderException>(async () => await reader.ReadAsync().ConfigureAwait(true), "Unexpected end when reading JSON. Path '', line 1, position 2.").ConfigureAwait(true);
        }

        [Test]
        public async Task UnexpectedEndAfterReadingNeAsync()
        {
            JsonTextReader reader = new JsonTextReader(new StringReader("ne"));
            await ExceptionAssert.ThrowsAsync<JsonReaderException>(async () => await reader.ReadAsync().ConfigureAwait(true), "Unexpected end when reading JSON. Path '', line 1, position 2.").ConfigureAwait(true);
        }

        [Test]
        public async Task UnexpectedEndOfHexAsync()
        {
            JsonReader reader = new JsonTextReader(new StringReader(@"'h\u123"));

            await ExceptionAssert.ThrowsAsync<JsonReaderException>(async () => { await reader.ReadAsync().ConfigureAwait(true); }, "Unexpected end while parsing unicode character. Path '', line 1, position 4.").ConfigureAwait(true);
        }

        [Test]
        public async Task UnexpectedEndOfControlCharacterAsync()
        {
            JsonReader reader = new JsonTextReader(new StringReader(@"'h\"));

            await ExceptionAssert.ThrowsAsync<JsonReaderException>(async () => { await reader.ReadAsync().ConfigureAwait(true); }, "Unterminated string. Expected delimiter: '. Path '', line 1, position 3.").ConfigureAwait(true);
        }

        [Test]
        public async Task ReadInvalidNonBase10NumberAsync()
        {
            string json = "0aq2dun13.hod";

            JsonTextReader reader = new JsonTextReader(new StringReader(json));

            await ExceptionAssert.ThrowsAsync<JsonReaderException>(async () => { await reader.ReadAsync().ConfigureAwait(true); }, "Unexpected character encountered while parsing number: q. Path '', line 1, position 2.").ConfigureAwait(true);

            reader = new JsonTextReader(new StringReader(json));

            await ExceptionAssert.ThrowsAsync<JsonReaderException>(async () => { await reader.ReadAsDecimalAsync().ConfigureAwait(true); }, "Unexpected character encountered while parsing number: q. Path '', line 1, position 2.").ConfigureAwait(true);

            reader = new JsonTextReader(new StringReader(json));

            await ExceptionAssert.ThrowsAsync<JsonReaderException>(async () => { await reader.ReadAsInt32Async().ConfigureAwait(true); }, "Unexpected character encountered while parsing number: q. Path '', line 1, position 2.").ConfigureAwait(true);
        }

        [Test]
        public async Task ThrowErrorWhenParsingUnquoteStringThatStartsWithNEAsync()
        {
            const string json = @"{ ""ItemName"": ""value"", ""u"":netanelsalinger,""r"":9 }";

            JsonTextReader reader = new JsonTextReader(new StringReader(json));

            Assert.IsTrue(await reader.ReadAsync().ConfigureAwait(true));
            Assert.AreEqual(JsonToken.StartObject, reader.TokenType);

            Assert.IsTrue(await reader.ReadAsync().ConfigureAwait(true));
            Assert.AreEqual(JsonToken.PropertyName, reader.TokenType);

            Assert.IsTrue(await reader.ReadAsync().ConfigureAwait(true));
            Assert.AreEqual(JsonToken.String, reader.TokenType);

            Assert.IsTrue(await reader.ReadAsync().ConfigureAwait(true));
            Assert.AreEqual(JsonToken.PropertyName, reader.TokenType);

            await ExceptionAssert.ThrowsAsync<JsonReaderException>(async () => { await reader.ReadAsync().ConfigureAwait(true); }, "Unexpected content while parsing JSON. Path 'u', line 1, position 29.").ConfigureAwait(true);
        }

        [Test]
        public async Task UnexpectedEndOfStringAsync()
        {
            JsonReader reader = new JsonTextReader(new StringReader("'hi"));

            await ExceptionAssert.ThrowsAsync<JsonReaderException>(async () => { await reader.ReadAsync().ConfigureAwait(true); }, "Unterminated string. Expected delimiter: '. Path '', line 1, position 3.").ConfigureAwait(true);
        }

        [Test]
        public async Task UnexpectedEndTokenWhenParsingOddEndTokenAsync()
        {
            JsonReader reader = new JsonTextReader(new StringReader(@"{}}"));
            Assert.IsTrue(await reader.ReadAsync().ConfigureAwait(true));
            Assert.IsTrue(await reader.ReadAsync().ConfigureAwait(true));

            await ExceptionAssert.ThrowsAsync<JsonReaderException>(async () => { await reader.ReadAsync().ConfigureAwait(true); }, "Additional text encountered after finished reading JSON content: }. Path '', line 1, position 2.").ConfigureAwait(true);
        }

        [Test]
        public async Task ResetJsonTextReaderErrorCountAsync()
        {
            ToggleReaderError toggleReaderError = new ToggleReaderError(new StringReader("{'first':1,'second':2,'third':3}"));
            JsonTextReader jsonTextReader = new JsonTextReader(toggleReaderError);

            Assert.IsTrue(await jsonTextReader.ReadAsync().ConfigureAwait(true));

            toggleReaderError.Error = true;

            await ExceptionAssert.ThrowsAsync<Exception>(async () => await jsonTextReader.ReadAsync().ConfigureAwait(true), "Read error").ConfigureAwait(true);
            await ExceptionAssert.ThrowsAsync<Exception>(async () => await jsonTextReader.ReadAsync().ConfigureAwait(true), "Read error").ConfigureAwait(true);

            toggleReaderError.Error = false;

            Assert.IsTrue(await jsonTextReader.ReadAsync().ConfigureAwait(true));
            Assert.AreEqual("first", jsonTextReader.Value);

            toggleReaderError.Error = true;

            await ExceptionAssert.ThrowsAsync<Exception>(async () => await jsonTextReader.ReadAsync().ConfigureAwait(true), "Read error").ConfigureAwait(true);

            toggleReaderError.Error = false;

            Assert.IsTrue(await jsonTextReader.ReadAsync().ConfigureAwait(true));
            Assert.AreEqual(1L, jsonTextReader.Value);

            toggleReaderError.Error = true;

            await ExceptionAssert.ThrowsAsync<Exception>(async () => await jsonTextReader.ReadAsync().ConfigureAwait(true), "Read error").ConfigureAwait(true);
            await ExceptionAssert.ThrowsAsync<Exception>(async () => await jsonTextReader.ReadAsync().ConfigureAwait(true), "Read error").ConfigureAwait(true);
            await ExceptionAssert.ThrowsAsync<Exception>(async () => await jsonTextReader.ReadAsync().ConfigureAwait(true), "Read error").ConfigureAwait(true);

            toggleReaderError.Error = false;
        }

        [Test]
        public async Task MatchWithInsufficentCharactersAsync()
        {
            JsonTextReader reader = new JsonTextReader(new StringReader(@"nul"));

            await ExceptionAssert.ThrowsAsync<JsonReaderException>(async () => { await reader.ReadAsync().ConfigureAwait(true); }, "Unexpected end when reading JSON. Path '', line 1, position 3.").ConfigureAwait(true);
        }

        [Test]
        public async Task MatchWithWrongCharactersAsync()
        {
            JsonTextReader reader = new JsonTextReader(new StringReader(@"nulz"));

            await ExceptionAssert.ThrowsAsync<JsonReaderException>(async () => { await reader.ReadAsync().ConfigureAwait(true); }, "Error parsing null value. Path '', line 1, position 3.").ConfigureAwait(true);
        }

        [Test]
        public async Task MatchWithNoTrailingSeparatorAsync()
        {
            JsonTextReader reader = new JsonTextReader(new StringReader(@"nullz"));

            await ExceptionAssert.ThrowsAsync<JsonReaderException>(async () => { await reader.ReadAsync().ConfigureAwait(true); }, "Error parsing null value. Path '', line 1, position 4.").ConfigureAwait(true);
        }

        [Test]
        public async Task UnclosedCommentAsync()
        {
            JsonTextReader reader = new JsonTextReader(new StringReader(@"/* sdf"));

            await ExceptionAssert.ThrowsAsync<JsonReaderException>(async () => { await reader.ReadAsync().ConfigureAwait(true); }, "Unexpected end while parsing comment. Path '', line 1, position 6.").ConfigureAwait(true);
        }

        [Test]
        public async Task BadCommentStartAsync()
        {
            JsonTextReader reader = new JsonTextReader(new StringReader(@"/sdf"));

            await ExceptionAssert.ThrowsAsync<JsonReaderException>(async () => { await reader.ReadAsync().ConfigureAwait(true); }, "Error parsing comment. Expected: *, got s. Path '', line 1, position 1.").ConfigureAwait(true);
        }

        [Test]
        public async Task MissingColonAsync()
        {
            string json = @"{
    ""A"" : true,
    ""B"" """;

            JsonTextReader reader = new JsonTextReader(new StringReader(json));

            await reader.ReadAsync().ConfigureAwait(true);
            Assert.AreEqual(JsonToken.StartObject, reader.TokenType);

            await reader.ReadAsync().ConfigureAwait(true);
            Assert.AreEqual(JsonToken.PropertyName, reader.TokenType);

            await reader.ReadAsync().ConfigureAwait(true);
            Assert.AreEqual(JsonToken.Boolean, reader.TokenType);

            await ExceptionAssert.ThrowsAsync<JsonReaderException>(async () => { await reader.ReadAsync().ConfigureAwait(true); }, @"Invalid character after parsing property name. Expected ':' but got: "". Path 'A', line 3, position 8.").ConfigureAwait(true);
        }

        [Test]
        public async Task ParseConstructorWithBadCharacterAsync()
        {
            string json = "new Date,()";
            JsonTextReader reader = new JsonTextReader(new StringReader(json));

            await ExceptionAssert.ThrowsAsync<JsonReaderException>(async () => { Assert.IsTrue(await reader.ReadAsync().ConfigureAwait(true)); }, "Unexpected character while parsing constructor: ,. Path '', line 1, position 8.").ConfigureAwait(true);
        }

        [Test]
        public async Task ParseConstructorWithUnexpectedEndAsync()
        {
            string json = "new Dat";
            JsonTextReader reader = new JsonTextReader(new StringReader(json));

            await ExceptionAssert.ThrowsAsync<JsonReaderException>(async () => { await reader.ReadAsync().ConfigureAwait(true); }, "Unexpected end while parsing constructor. Path '', line 1, position 7.").ConfigureAwait(true);
        }

        [Test]
        public async Task ParseConstructorWithUnexpectedCharacterAsync()
        {
            string json = "new Date !";
            JsonTextReader reader = new JsonTextReader(new StringReader(json));

            await ExceptionAssert.ThrowsAsync<JsonReaderException>(async () => { await reader.ReadAsync().ConfigureAwait(true); }, "Unexpected character while parsing constructor: !. Path '', line 1, position 9.").ConfigureAwait(true);
        }

        [Test]
        public async Task ParseAdditionalContent_CommaAsync()
        {
            string json = @"[
""Small"",
""Medium"",
""Large""
],";

            JsonTextReader reader = new JsonTextReader(new StringReader(json));

            await ExceptionAssert.ThrowsAsync<JsonReaderException>(async () =>
            {
                while (await reader.ReadAsync().ConfigureAwait(true))
                {
                }
            }, "Additional text encountered after finished reading JSON content: ,. Path '', line 5, position 1.").ConfigureAwait(true);
        }

        [Test]
        public async Task ParseAdditionalContent_TextAsync()
        {
            string json = @"[
""Small"",
""Medium"",
""Large""
]content";

            JsonTextReader reader = new JsonTextReader(new StringReader(json));
#if DEBUG
            reader.SetCharBuffer(new char[2]);
#endif

            await reader.ReadAsync().ConfigureAwait(true);
            Assert.AreEqual(1, reader.LineNumber);

            await reader.ReadAsync().ConfigureAwait(true);
            Assert.AreEqual(2, reader.LineNumber);

            await reader.ReadAsync().ConfigureAwait(true);
            Assert.AreEqual(3, reader.LineNumber);

            await reader.ReadAsync().ConfigureAwait(true);
            Assert.AreEqual(4, reader.LineNumber);

            await reader.ReadAsync().ConfigureAwait(true);
            Assert.AreEqual(5, reader.LineNumber);

            await ExceptionAssert.ThrowsAsync<JsonReaderException>(async () => { await reader.ReadAsync().ConfigureAwait(true); }, "Additional text encountered after finished reading JSON content: c. Path '', line 5, position 1.").ConfigureAwait(true);
        }

        [Test]
        public async Task ParseAdditionalContent_WhitespaceThenTextAsync()
        {
            string json = @"'hi' a";

            JsonTextReader reader = new JsonTextReader(new StringReader(json));

            await ExceptionAssert.ThrowsAsync<JsonReaderException>(async () =>
            {
                while (await reader.ReadAsync().ConfigureAwait(true))
                {
                }
            }, "Additional text encountered after finished reading JSON content: a. Path '', line 1, position 5.").ConfigureAwait(true);
        }

        [Test]
        public async Task ParseIncompleteCommentSeparatorAsync()
        {
            JsonTextReader reader = new JsonTextReader(new StringReader("true/"));

            await ExceptionAssert.ThrowsAsync<JsonReaderException>(async () => { await reader.ReadAsync().ConfigureAwait(true); }, "Error parsing boolean value. Path '', line 1, position 4.").ConfigureAwait(true);
        }

        [Test]
        public async Task ReadBadCharInArrayAsync()
        {
            JsonTextReader reader = new JsonTextReader(new StringReader(@"[}"));

            await reader.ReadAsync().ConfigureAwait(true);

            await ExceptionAssert.ThrowsAsync<JsonReaderException>(async () => { await reader.ReadAsync().ConfigureAwait(true); }, "Unexpected character encountered while parsing value: }. Path '', line 1, position 1.").ConfigureAwait(true);
        }

        [Test]
        public async Task ReadAsBytesNoContentWrappedObjectAsync()
        {
            JsonTextReader reader = new JsonTextReader(new StringReader(@"{"));

            await ExceptionAssert.ThrowsAsync<JsonReaderException>(async () => { await reader.ReadAsBytesAsync().ConfigureAwait(true); }, "Unexpected end when reading JSON. Path '', line 1, position 1.").ConfigureAwait(true);
        }

        [Test]
        public async Task ReadBytesEmptyWrappedObjectAsync()
        {
            JsonTextReader reader = new JsonTextReader(new StringReader(@"{}"));

            await ExceptionAssert.ThrowsAsync<JsonReaderException>(async () => { await reader.ReadAsBytesAsync().ConfigureAwait(true); }, "Error reading bytes. Unexpected token: StartObject. Path '', line 1, position 2." ).ConfigureAwait(true);
        }

        [Test]
        public async Task ReadIntegerWithErrorAsync()
        {
            string json = @"{
    ChildId: 333333333333333333333333333333333333333
}";

            JsonTextReader jsonTextReader = new JsonTextReader(new StringReader(json));

            Assert.IsTrue(await jsonTextReader.ReadAsync().ConfigureAwait(true));
            Assert.AreEqual(JsonToken.StartObject, jsonTextReader.TokenType);

            Assert.IsTrue(await jsonTextReader.ReadAsync().ConfigureAwait(true));
            Assert.AreEqual(JsonToken.PropertyName, jsonTextReader.TokenType);

            await ExceptionAssert.ThrowsAsync<JsonReaderException>(async () => await jsonTextReader.ReadAsInt32Async().ConfigureAwait(true), "JSON integer 333333333333333333333333333333333333333 is too large or small for an Int32. Path 'ChildId', line 2, position 52.").ConfigureAwait(true);

            Assert.IsTrue(await jsonTextReader.ReadAsync().ConfigureAwait(true));
            Assert.AreEqual(JsonToken.EndObject, jsonTextReader.TokenType);

            Assert.IsFalse(await jsonTextReader.ReadAsync().ConfigureAwait(true));
        }

        [Test]
        public async Task ReadIntegerWithErrorInArrayAsync()
        {
            string json = @"[
  333333333333333333333333333333333333333,
  3.3,
  ,
  0f
]";

            JsonTextReader jsonTextReader = new JsonTextReader(new StringReader(json));

            Assert.IsTrue(await jsonTextReader.ReadAsync().ConfigureAwait(true));
            Assert.AreEqual(JsonToken.StartArray, jsonTextReader.TokenType);

            await ExceptionAssert.ThrowsAsync<JsonReaderException>(async () => await jsonTextReader.ReadAsInt32Async().ConfigureAwait(true), "JSON integer 333333333333333333333333333333333333333 is too large or small for an Int32. Path '[0]', line 2, position 41.").ConfigureAwait(true);

            await ExceptionAssert.ThrowsAsync<JsonReaderException>(async () => await jsonTextReader.ReadAsInt32Async().ConfigureAwait(true), "Input string '3.3' is not a valid integer. Path '[1]', line 3, position 5.").ConfigureAwait(true);

            await ExceptionAssert.ThrowsAsync<JsonReaderException>(async () => await jsonTextReader.ReadAsInt32Async().ConfigureAwait(true), "Unexpected character encountered while parsing value: ,. Path '[2]', line 4, position 3.").ConfigureAwait(true);

            await ExceptionAssert.ThrowsAsync<JsonReaderException>(async () => await jsonTextReader.ReadAsInt32Async().ConfigureAwait(true), "Input string '0f' is not a valid integer. Path '[3]', line 5, position 4.").ConfigureAwait(true);

            Assert.IsTrue(await jsonTextReader.ReadAsync().ConfigureAwait(true));
            Assert.AreEqual(JsonToken.EndArray, jsonTextReader.TokenType);

            Assert.IsFalse(await jsonTextReader.ReadAsync().ConfigureAwait(true));
        }

        [Test]
        public async Task ReadBytesWithErrorAsync()
        {
            string json = @"{
    ChildId: '123'
}";

            JsonTextReader jsonTextReader = new JsonTextReader(new StringReader(json));

            Assert.IsTrue(await jsonTextReader.ReadAsync().ConfigureAwait(true));
            Assert.AreEqual(JsonToken.StartObject, jsonTextReader.TokenType);

            Assert.IsTrue(await jsonTextReader.ReadAsync().ConfigureAwait(true));
            Assert.AreEqual(JsonToken.PropertyName, jsonTextReader.TokenType);

            try
            {
                await jsonTextReader.ReadAsBytesAsync().ConfigureAwait(true);
            }
            catch (FormatException)
            {
            }

            Assert.IsTrue(await jsonTextReader.ReadAsync().ConfigureAwait(true));
            Assert.AreEqual(JsonToken.EndObject, jsonTextReader.TokenType);

            Assert.IsFalse(await jsonTextReader.ReadAsync().ConfigureAwait(true));
        }

        [Test]
        public async Task ReadInt32OverflowAsync()
        {
            long i = int.MaxValue;

            JsonTextReader reader = new JsonTextReader(new StringReader(i.ToString(CultureInfo.InvariantCulture)));
            await reader.ReadAsync().ConfigureAwait(true);
            Assert.AreEqual(typeof(long), reader.ValueType);

            for (int j = 1; j < 1000; j++)
            {
                long total = j + i;
                await ExceptionAssert.ThrowsAsync<JsonReaderException>(async () =>
                {
                    reader = new JsonTextReader(new StringReader(total.ToString(CultureInfo.InvariantCulture)));
                    await reader.ReadAsInt32Async().ConfigureAwait(true);
                }, "JSON integer " + total + " is too large or small for an Int32. Path '', line 1, position 10.").ConfigureAwait(true);
            }
        }

        [Test]
        public async Task ReadInt32Overflow_NegativeAsync()
        {
            long i = int.MinValue;

            JsonTextReader reader = new JsonTextReader(new StringReader(i.ToString(CultureInfo.InvariantCulture)));
            await reader.ReadAsync().ConfigureAwait(true);
            Assert.AreEqual(typeof(long), reader.ValueType);
            Assert.AreEqual(i, reader.Value);

            for (int j = 1; j < 1000; j++)
            {
                long total = -j + i;
                await ExceptionAssert.ThrowsAsync<JsonReaderException>(async () =>
                {
                    reader = new JsonTextReader(new StringReader(total.ToString(CultureInfo.InvariantCulture)));
                    await reader.ReadAsInt32Async().ConfigureAwait(true);
                }, "JSON integer " + total + " is too large or small for an Int32. Path '', line 1, position 11.").ConfigureAwait(true);
            }
        }

#if !PORTABLE || NETSTANDARD1_1
        [Test]
        public async Task ReadInt64OverflowAsync()
        {
            BigInteger i = new BigInteger(long.MaxValue);

            JsonTextReader reader = new JsonTextReader(new StringReader(i.ToString(CultureInfo.InvariantCulture)));
            await reader.ReadAsync().ConfigureAwait(true);
            Assert.AreEqual(typeof(long), reader.ValueType);

            for (int j = 1; j < 1000; j++)
            {
                BigInteger total = i + j;

                reader = new JsonTextReader(new StringReader(total.ToString(CultureInfo.InvariantCulture)));
                await reader.ReadAsync().ConfigureAwait(true);

                Assert.AreEqual(typeof(BigInteger), reader.ValueType);
            }
        }

        [Test]
        public async Task ReadInt64Overflow_NegativeAsync()
        {
            BigInteger i = new BigInteger(long.MinValue);

            JsonTextReader reader = new JsonTextReader(new StringReader(i.ToString(CultureInfo.InvariantCulture)));
            await reader.ReadAsync().ConfigureAwait(true);
            Assert.AreEqual(typeof(long), reader.ValueType);

            for (int j = 1; j < 1000; j++)
            {
                BigInteger total = i + -j;

                reader = new JsonTextReader(new StringReader(total.ToString(CultureInfo.InvariantCulture)));
                await reader.ReadAsync().ConfigureAwait(true);

                Assert.AreEqual(typeof(BigInteger), reader.ValueType);
            }
        }
#endif

        [Test]
        public async Task ReadAsString_Null_AdditionalBadDataAsync()
        {
            string json = @"nullllll";

            JsonTextReader reader = new JsonTextReader(new StringReader(json));

            await ExceptionAssert.ThrowsAsync<JsonReaderException>(async () => { await reader.ReadAsStringAsync().ConfigureAwait(true); }, "Error parsing null value. Path '', line 1, position 4.").ConfigureAwait(true);
        }

        [Test]
        public async Task ReadAsBoolean_AdditionalBadDataAsync()
        {
            string json = @"falseeeee";

            JsonTextReader reader = new JsonTextReader(new StringReader(json));

            await ExceptionAssert.ThrowsAsync<JsonReaderException>(async () => { await reader.ReadAsBooleanAsync().ConfigureAwait(true); }, "Unexpected character encountered while parsing value: e. Path '', line 1, position 5.").ConfigureAwait(true);
        }

        [Test]
        public async Task ReadAsString_AdditionalBadDataAsync()
        {
            string json = @"falseeeee";

            JsonTextReader reader = new JsonTextReader(new StringReader(json));

            await ExceptionAssert.ThrowsAsync<JsonReaderException>(async () => { await reader.ReadAsStringAsync().ConfigureAwait(true); }, "Unexpected character encountered while parsing value: e. Path '', line 1, position 5.").ConfigureAwait(true);
        }

        [Test]
        public async Task ReadAsBoolean_UnexpectedEndAsync()
        {
            string json = @"tru";

            JsonTextReader reader = new JsonTextReader(new StringReader(json));

            await ExceptionAssert.ThrowsAsync<JsonReaderException>(async () => { await reader.ReadAsBooleanAsync().ConfigureAwait(true); }, "Unexpected end when reading JSON. Path '', line 1, position 3.").ConfigureAwait(true);
        }

        [Test]
        public async Task ReadAsBoolean_BadDataAsync()
        {
            string json = @"pie";

            JsonTextReader reader = new JsonTextReader(new StringReader(json));

            await ExceptionAssert.ThrowsAsync<JsonReaderException>(async () => { await reader.ReadAsBooleanAsync().ConfigureAwait(true); }, "Unexpected character encountered while parsing value: p. Path '', line 1, position 1.").ConfigureAwait(true);
        }

        [Test]
        public async Task ReadAsString_BadDataAsync()
        {
            string json = @"pie";

            JsonTextReader reader = new JsonTextReader(new StringReader(json));

            await ExceptionAssert.ThrowsAsync<JsonReaderException>(async () => { await reader.ReadAsStringAsync().ConfigureAwait(true); }, "Unexpected character encountered while parsing value: p. Path '', line 1, position 1.").ConfigureAwait(true);
        }

        [Test]
        public async Task ReadAsDouble_BadDataAsync()
        {
            string json = @"pie";

            JsonTextReader reader = new JsonTextReader(new StringReader(json));

            await ExceptionAssert.ThrowsAsync<JsonReaderException>(async () => { await reader.ReadAsDoubleAsync().ConfigureAwait(true); }, "Unexpected character encountered while parsing value: p. Path '', line 1, position 1.").ConfigureAwait(true);
        }

        [Test]
        public async Task ReadAsDouble_BooleanAsync()
        {
            string json = @"true";

            JsonTextReader reader = new JsonTextReader(new StringReader(json));

            await ExceptionAssert.ThrowsAsync<JsonReaderException>(async () => { await reader.ReadAsDoubleAsync().ConfigureAwait(true); }, "Unexpected character encountered while parsing value: t. Path '', line 1, position 1.").ConfigureAwait(true);
        }

        [Test]
        public async Task ReadAsBytes_BadDataAsync()
        {
            string json = @"pie";

            JsonTextReader reader = new JsonTextReader(new StringReader(json));

            await ExceptionAssert.ThrowsAsync<JsonReaderException>(async () => { await reader.ReadAsBytesAsync().ConfigureAwait(true); }, "Unexpected character encountered while parsing value: p. Path '', line 1, position 1.").ConfigureAwait(true);
        }

        [Test]
        public async Task ReadAsBytesIntegerArrayWithNoEndAsync()
        {
            JsonTextReader reader = new JsonTextReader(new StringReader(@"[1"));

            await ExceptionAssert.ThrowsAsync<JsonReaderException>(async () => { await reader.ReadAsBytesAsync().ConfigureAwait(true); }, "Unexpected end when reading bytes. Path '[0]', line 1, position 2.").ConfigureAwait(true);
        }

        [Test]
        public async Task ReadAsBytesArrayWithBadContentAsync()
        {
            JsonTextReader reader = new JsonTextReader(new StringReader(@"[1.0]"));

            await ExceptionAssert.ThrowsAsync<JsonReaderException>(async () => { await reader.ReadAsBytesAsync().ConfigureAwait(true); }, "Unexpected token when reading bytes: Float. Path '[0]', line 1, position 4.").ConfigureAwait(true);
        }

        [Test]
        public async Task ReadAsBytesBadContentAsync()
        {
            JsonTextReader reader = new JsonTextReader(new StringReader(@"new Date()"));

            await ExceptionAssert.ThrowsAsync<JsonReaderException>(async () => { await reader.ReadAsBytesAsync().ConfigureAwait(true); }, "Unexpected character encountered while parsing value: e. Path '', line 1, position 2.").ConfigureAwait(true);
        }

        [Test]
        public async Task ReadAsBytes_CommaErrorsAsync()
        {
            JsonTextReader reader = new JsonTextReader(new StringReader("[,'']"));
            await reader.ReadAsync().ConfigureAwait(true);

            await ExceptionAssert.ThrowsAsync<JsonReaderException>(async () =>
            {
                await reader.ReadAsBytesAsync().ConfigureAwait(true);
            }, "Unexpected character encountered while parsing value: ,. Path '[0]', line 1, position 2.").ConfigureAwait(true);

            CollectionAssert.AreEquivalent(new byte[0], await reader.ReadAsBytesAsync().ConfigureAwait(true));
            Assert.IsTrue(await reader.ReadAsync().ConfigureAwait(true));
        }

        [Test]
        public async Task ReadAsBytes_InvalidEndArrayAsync()
        {
            JsonTextReader reader = new JsonTextReader(new StringReader("]"));

            await ExceptionAssert.ThrowsAsync<JsonReaderException>(async () =>
            {
                await reader.ReadAsBytesAsync().ConfigureAwait(true);
            }, "Unexpected character encountered while parsing value: ]. Path '', line 1, position 1.").ConfigureAwait(true);
        }

        [Test]
        public async Task ReadAsBytes_CommaErrors_MultipleAsync()
        {
            JsonTextReader reader = new JsonTextReader(new StringReader("['',,'']"));
            await reader.ReadAsync().ConfigureAwait(true);
            CollectionAssert.AreEquivalent(new byte[0], await reader.ReadAsBytesAsync().ConfigureAwait(true));

            await ExceptionAssert.ThrowsAsync<JsonReaderException>(async () =>
            {
                await reader.ReadAsBytesAsync().ConfigureAwait(true);
            }, "Unexpected character encountered while parsing value: ,. Path '[1]', line 1, position 5.").ConfigureAwait(true);

            CollectionAssert.AreEquivalent(new byte[0], await reader.ReadAsBytesAsync().ConfigureAwait(true));
            Assert.IsTrue(await reader.ReadAsync().ConfigureAwait(true));
        }

        [Test]
        public async Task ReadBytesWithBadCharacterAsync()
        {
            JsonReader reader = new JsonTextReader(new StringReader(@"true"));

            await ExceptionAssert.ThrowsAsync<JsonReaderException>(async () => { await reader.ReadAsBytesAsync().ConfigureAwait(true); }, "Unexpected character encountered while parsing value: t. Path '', line 1, position 1.").ConfigureAwait(true);
        }

        [Test]
        public async Task ReadBytesWithUnexpectedEndAsync()
        {
            string helloWorld = "Hello world!";
            byte[] helloWorldData = Encoding.UTF8.GetBytes(helloWorld);

            JsonReader reader = new JsonTextReader(new StringReader(@"'" + Convert.ToBase64String(helloWorldData)));

            await ExceptionAssert.ThrowsAsync<JsonReaderException>(async () => { await reader.ReadAsBytesAsync().ConfigureAwait(true); }, "Unterminated string. Expected delimiter: '. Path '', line 1, position 17.").ConfigureAwait(true);
        }

        [Test]
        public async Task ReadAsDateTime_BadDataAsync()
        {
            string json = @"pie";

            JsonTextReader reader = new JsonTextReader(new StringReader(json));

            await ExceptionAssert.ThrowsAsync<JsonReaderException>(async () => { await reader.ReadAsDateTimeAsync().ConfigureAwait(true); }, "Unexpected character encountered while parsing value: p. Path '', line 1, position 1.").ConfigureAwait(true);
        }

        [Test]
        public async Task ReadAsDateTime_BooleanAsync()
        {
            string json = @"true";

            JsonTextReader reader = new JsonTextReader(new StringReader(json));

            await ExceptionAssert.ThrowsAsync<JsonReaderException>(async () => { await reader.ReadAsDateTimeAsync().ConfigureAwait(true); }, "Unexpected character encountered while parsing value: t. Path '', line 1, position 1.").ConfigureAwait(true);
        }

#if !NET20
        [Test]
        public async Task ReadAsDateTimeOffsetBadContentAsync()
        {
            JsonTextReader reader = new JsonTextReader(new StringReader(@"new Date()"));

            await ExceptionAssert.ThrowsAsync<JsonReaderException>(async () => { await reader.ReadAsDateTimeOffsetAsync().ConfigureAwait(true); }, "Unexpected character encountered while parsing value: e. Path '', line 1, position 2.").ConfigureAwait(true);
        }
#endif

        [Test]
        public async Task ReadAsDecimalBadContentAsync()
        {
            JsonTextReader reader = new JsonTextReader(new StringReader(@"new Date()"));

            await ExceptionAssert.ThrowsAsync<JsonReaderException>(async () => { await reader.ReadAsDecimalAsync().ConfigureAwait(true); }, "Unexpected character encountered while parsing value: e. Path '', line 1, position 2.").ConfigureAwait(true);
        }

        [Test]
        public async Task ReadAsDecimalBadContent_SecondLineAsync()
        {
            JsonTextReader reader = new JsonTextReader(new StringReader(@"
new Date()"));

            await ExceptionAssert.ThrowsAsync<JsonReaderException>(async () => { await reader.ReadAsDecimalAsync().ConfigureAwait(true); }, "Unexpected character encountered while parsing value: e. Path '', line 2, position 2.").ConfigureAwait(true);
        }

        [Test]
        public async Task ReadInt32WithBadCharacterAsync()
        {
            JsonReader reader = new JsonTextReader(new StringReader(@"true"));

            await ExceptionAssert.ThrowsAsync<JsonReaderException>(async () => { await reader.ReadAsInt32Async().ConfigureAwait(true); }, "Unexpected character encountered while parsing value: t. Path '', line 1, position 1.").ConfigureAwait(true);
        }

        [Test]
        public async Task ReadNumberValue_CommaErrorsAsync()
        {
            JsonTextReader reader = new JsonTextReader(new StringReader("[,1]"));
            await reader.ReadAsync().ConfigureAwait(true);

            await ExceptionAssert.ThrowsAsync<JsonReaderException>(async () =>
            {
                await reader.ReadAsInt32Async().ConfigureAwait(true);
            }, "Unexpected character encountered while parsing value: ,. Path '[0]', line 1, position 2.").ConfigureAwait(true);

            Assert.AreEqual(1, await reader.ReadAsInt32Async().ConfigureAwait(true));
            Assert.IsTrue(await reader.ReadAsync().ConfigureAwait(true));
        }

        [Test]
        public async Task ReadNumberValue_InvalidEndArrayAsync()
        {
            JsonTextReader reader = new JsonTextReader(new StringReader("]"));

            await ExceptionAssert.ThrowsAsync<JsonReaderException>(async () =>
            {
                await reader.ReadAsInt32Async().ConfigureAwait(true);
            }, "Unexpected character encountered while parsing value: ]. Path '', line 1, position 1.").ConfigureAwait(true);
        }

        [Test]
        public async Task ReadNumberValue_CommaErrors_MultipleAsync()
        {
            JsonTextReader reader = new JsonTextReader(new StringReader("[1,,1]"));
            await reader.ReadAsync().ConfigureAwait(true);
            await reader.ReadAsInt32Async().ConfigureAwait(true);

            await ExceptionAssert.ThrowsAsync<JsonReaderException>(async () =>
            {
                await reader.ReadAsInt32Async().ConfigureAwait(true);
            }, "Unexpected character encountered while parsing value: ,. Path '[1]', line 1, position 4.").ConfigureAwait(true);

            Assert.AreEqual(1, await reader.ReadAsInt32Async().ConfigureAwait(true));
            Assert.IsTrue(await reader.ReadAsync().ConfigureAwait(true));
        }

        [Test]
        public async Task ReadAsString_UnexpectedEndAsync()
        {
            string json = @"tru";

            JsonTextReader reader = new JsonTextReader(new StringReader(json));

            await ExceptionAssert.ThrowsAsync<JsonReaderException>(async () => { await reader.ReadAsStringAsync().ConfigureAwait(true); }, "Unexpected end when reading JSON. Path '', line 1, position 3.").ConfigureAwait(true);
        }

        [Test]
        public async Task ReadAsString_Null_UnexpectedEndAsync()
        {
            string json = @"nul";

            JsonTextReader reader = new JsonTextReader(new StringReader(json));

            await ExceptionAssert.ThrowsAsync<JsonReaderException>(async () => { await reader.ReadAsStringAsync().ConfigureAwait(true); }, "Unexpected end when reading JSON. Path '', line 1, position 3.").ConfigureAwait(true);
        }

        [Test]
        public async Task ReadStringValue_InvalidEndArrayAsync()
        {
            JsonTextReader reader = new JsonTextReader(new StringReader("]"));

            await ExceptionAssert.ThrowsAsync<JsonReaderException>(async () =>
            {
                await reader.ReadAsDateTimeAsync().ConfigureAwait(true);
            }, "Unexpected character encountered while parsing value: ]. Path '', line 1, position 1.").ConfigureAwait(true);
        }

        [Test]
        public async Task ReadStringValue_CommaErrorsAsync()
        {
            JsonTextReader reader = new JsonTextReader(new StringReader("[,'']"));
            await reader.ReadAsync().ConfigureAwait(true);

            await ExceptionAssert.ThrowsAsync<JsonReaderException>(async () =>
            {
                await reader.ReadAsStringAsync().ConfigureAwait(true);
            }, "Unexpected character encountered while parsing value: ,. Path '[0]', line 1, position 2.").ConfigureAwait(true);

            Assert.AreEqual(string.Empty, await reader.ReadAsStringAsync().ConfigureAwait(true));
            Assert.IsTrue(await reader.ReadAsync().ConfigureAwait(true));
        }

        [Test]
        public async Task ReadStringValue_CommaErrors_MultipleAsync()
        {
            JsonTextReader reader = new JsonTextReader(new StringReader("['',,'']"));
            await reader.ReadAsync().ConfigureAwait(true);
            await reader.ReadAsInt32Async().ConfigureAwait(true);

            await ExceptionAssert.ThrowsAsync<JsonReaderException>(async () =>
            {
                await reader.ReadAsStringAsync().ConfigureAwait(true);
            }, "Unexpected character encountered while parsing value: ,. Path '[1]', line 1, position 5.").ConfigureAwait(true);

            Assert.AreEqual(string.Empty, await reader.ReadAsStringAsync().ConfigureAwait(true));
            Assert.IsTrue(await reader.ReadAsync().ConfigureAwait(true));
        }

        [Test]
        public async Task ReadStringValue_Numbers_NotStringAsync()
        {
            JsonTextReader reader = new JsonTextReader(new StringReader("[56,56]"));
            await reader.ReadAsync().ConfigureAwait(true);

            await ExceptionAssert.ThrowsAsync<JsonReaderException>(async () =>
            {
                await reader.ReadAsDateTimeAsync().ConfigureAwait(true);
            }, "Unexpected character encountered while parsing value: 5. Path '', line 1, position 2.").ConfigureAwait(true);

            await ExceptionAssert.ThrowsAsync<JsonReaderException>(async () =>
            {
                await reader.ReadAsDateTimeAsync().ConfigureAwait(true);
            }, "Unexpected character encountered while parsing value: 6. Path '', line 1, position 3.").ConfigureAwait(true);

            await ExceptionAssert.ThrowsAsync<JsonReaderException>(async () =>
            {
                await reader.ReadAsDateTimeAsync().ConfigureAwait(true);
            }, "Unexpected character encountered while parsing value: ,. Path '[0]', line 1, position 4.").ConfigureAwait(true);

            Assert.AreEqual(56, await reader.ReadAsInt32Async().ConfigureAwait(true));
            Assert.IsTrue(await reader.ReadAsync().ConfigureAwait(true));
        }

        [Test]
        public async Task ErrorReadingCommentAsync()
        {
            string json = @"/";

            JsonTextReader reader = new JsonTextReader(new StreamReader(new SlowStream(json, new UTF8Encoding(false), 1)));

            await ExceptionAssert.ThrowsAsync<JsonReaderException>(async () => { await reader.ReadAsync().ConfigureAwait(true); }, "Unexpected end while parsing comment. Path '', line 1, position 1.").ConfigureAwait(true);
        }

        [Test]
        public async Task EscapedPathInExceptionMessageAsync()
        {
            string json = @"{
  ""frameworks"": {
    ""dnxcore50"": {
      ""dependencies"": {
        ""System.Xml.ReaderWriter"": {
          ""source"": !!! !!!
        }
      }
    }
  }
}";

            await ExceptionAssert.ThrowsAsync<JsonReaderException>(
                async () =>
                {
                    JsonTextReader reader = new JsonTextReader(new StringReader(json));
                    while (await reader.ReadAsync().ConfigureAwait(true))
                    {
                    }
                },
                "Unexpected character encountered while parsing value: !. Path 'frameworks.dnxcore50.dependencies['System.Xml.ReaderWriter'].source', line 6, position 20.").ConfigureAwait(true);
        }

        [Test]
        public async Task MaxDepthAsync()
        {
            string json = "[[]]";

            JsonTextReader reader = new JsonTextReader(new StringReader(json))
            {
                MaxDepth = 1
            };

            Assert.IsTrue(await reader.ReadAsync().ConfigureAwait(true));

            await ExceptionAssert.ThrowsAsync<JsonReaderException>(async () => { Assert.IsTrue(await reader.ReadAsync().ConfigureAwait(true)); }, "The reader's MaxDepth of 1 has been exceeded. Path '[0]', line 1, position 2.").ConfigureAwait(true);
        }

        [Test]
        public async Task MaxDepthDoesNotRecursivelyErrorAsync()
        {
            string json = "[[[[]]],[[]]]";

            JsonTextReader reader = new JsonTextReader(new StringReader(json))
            {
                MaxDepth = 1
            };

            Assert.IsTrue(await reader.ReadAsync().ConfigureAwait(true));
            Assert.AreEqual(0, reader.Depth);

            await ExceptionAssert.ThrowsAsync<JsonReaderException>(async () => { Assert.IsTrue(await reader.ReadAsync().ConfigureAwait(true)); }, "The reader's MaxDepth of 1 has been exceeded. Path '[0]', line 1, position 2.").ConfigureAwait(true);
            Assert.AreEqual(1, reader.Depth);

            Assert.IsTrue(await reader.ReadAsync().ConfigureAwait(true));
            Assert.AreEqual(2, reader.Depth);

            Assert.IsTrue(await reader.ReadAsync().ConfigureAwait(true));
            Assert.AreEqual(3, reader.Depth);

            Assert.IsTrue(await reader.ReadAsync().ConfigureAwait(true));
            Assert.AreEqual(3, reader.Depth);

            Assert.IsTrue(await reader.ReadAsync().ConfigureAwait(true));
            Assert.AreEqual(2, reader.Depth);

            Assert.IsTrue(await reader.ReadAsync().ConfigureAwait(true));
            Assert.AreEqual(1, reader.Depth);

            await ExceptionAssert.ThrowsAsync<JsonReaderException>(async () => { Assert.IsTrue(await reader.ReadAsync().ConfigureAwait(true)); }, "The reader's MaxDepth of 1 has been exceeded. Path '[1]', line 1, position 9.").ConfigureAwait(true);
            Assert.AreEqual(1, reader.Depth);

            Assert.IsTrue(await reader.ReadAsync().ConfigureAwait(true));
            Assert.AreEqual(2, reader.Depth);

            Assert.IsTrue(await reader.ReadAsync().ConfigureAwait(true));
            Assert.AreEqual(2, reader.Depth);

            Assert.IsTrue(await reader.ReadAsync().ConfigureAwait(true));
            Assert.AreEqual(1, reader.Depth);

            Assert.IsTrue(await reader.ReadAsync().ConfigureAwait(true));
            Assert.AreEqual(0, reader.Depth);

            Assert.IsFalse(await reader.ReadAsync().ConfigureAwait(true));
        }

        [Test]
        public async Task UnexpectedEndWhenParsingUnquotedPropertyAsync()
        {
            JsonReader reader = new JsonTextReader(new StringReader(@"{aww"));
            Assert.IsTrue(await reader.ReadAsync().ConfigureAwait(true));

            await ExceptionAssert.ThrowsAsync<JsonReaderException>(async () => { await reader.ReadAsync().ConfigureAwait(true); }, "Unexpected end while parsing unquoted property name. Path '', line 1, position 4.").ConfigureAwait(true);
        }
    }
}

#endif
