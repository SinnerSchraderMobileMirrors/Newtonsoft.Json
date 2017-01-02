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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Tests.TestObjects.JsonTextReaderTests;
using Newtonsoft.Json.Utilities;
#if DNXCORE50
using Xunit;
using Test = Xunit.FactAttribute;
using Assert = Newtonsoft.Json.Tests.XUnitAssert;
#else
using NUnit.Framework;
#endif

namespace Newtonsoft.Json.Tests
{
    [TestFixture]
    public class JsonTextWriterAsyncTests : TestFixtureBase
    {
        [Test]
        public async Task BufferTestAsync()
        {
            FakeArrayPool arrayPool = new FakeArrayPool();

            string longString = new string('A', 2000);
            string longEscapedString = "Hello!" + new string('!', 50) + new string('\n', 1000) + "Good bye!";
            string longerEscapedString = "Hello!" + new string('!', 2000) + new string('\n', 1000) + "Good bye!";

            for (int i = 0; i < 1000; i++)
            {
                StringWriter sw = new StringWriter(CultureInfo.InvariantCulture);

                using (JsonTextWriter writer = new JsonTextWriter(sw))
                {
                    writer.ArrayPool = arrayPool;

                    await writer.WriteStartObjectAsync().ConfigureAwait(true);

                    await writer.WritePropertyNameAsync("Prop1").ConfigureAwait(true);
                    await writer.WriteValueAsync(new DateTime(2000, 12, 12, 12, 12, 12, DateTimeKind.Utc)).ConfigureAwait(true);

                    await writer.WritePropertyNameAsync("Prop2").ConfigureAwait(true);
                    await writer.WriteValueAsync(longString).ConfigureAwait(true);

                    await writer.WritePropertyNameAsync("Prop3").ConfigureAwait(true);
                    await writer.WriteValueAsync(longEscapedString).ConfigureAwait(true);

                    await writer.WritePropertyNameAsync("Prop4").ConfigureAwait(true);
                    await writer.WriteValueAsync(longerEscapedString).ConfigureAwait(true);

                    await writer.WriteEndObjectAsync().ConfigureAwait(true);
                }

                if ((i + 1) % 100 == 0)
                {
                    Console.WriteLine("Allocated buffers: " + arrayPool.FreeArrays.Count);
                }
            }

            Assert.AreEqual(0, arrayPool.UsedArrays.Count);
            Assert.AreEqual(3, arrayPool.FreeArrays.Count);
        }

        [Test]
        public async Task BufferTest_WithErrorAsync()
        {
            FakeArrayPool arrayPool = new FakeArrayPool();

            StringWriter sw = new StringWriter(CultureInfo.InvariantCulture);

            try
            {
                // dispose will free used buffers
                using (JsonTextWriter writer = new JsonTextWriter(sw))
                {
                    writer.ArrayPool = arrayPool;

                    await writer.WriteStartObjectAsync().ConfigureAwait(true);

                    await writer.WritePropertyNameAsync("Prop1").ConfigureAwait(true);
                    await writer.WriteValueAsync(new DateTime(2000, 12, 12, 12, 12, 12, DateTimeKind.Utc)).ConfigureAwait(true);

                    await writer.WritePropertyNameAsync("Prop2").ConfigureAwait(true);
                    await writer.WriteValueAsync("This is an escaped \n string!").ConfigureAwait(true);

                    await writer.WriteValueAsync("Error!").ConfigureAwait(true);
                }


                Assert.Fail();
            }
            catch
            {
            }

            Assert.AreEqual(0, arrayPool.UsedArrays.Count);
            Assert.AreEqual(1, arrayPool.FreeArrays.Count);
        }

        [Test]
        public async Task NewLineAsync()
        {
            MemoryStream ms = new MemoryStream();

            using (var streamWriter = new StreamWriter(ms, new UTF8Encoding(false)) { NewLine = "\n" })
            using (var jsonWriter = new JsonTextWriter(streamWriter)
            {
                CloseOutput = true,
                Indentation = 2,
                Formatting = Formatting.Indented
            })
            {
                await jsonWriter.WriteStartObjectAsync().ConfigureAwait(true);
                await jsonWriter.WritePropertyNameAsync("prop").ConfigureAwait(true);
                await jsonWriter.WriteValueAsync(true).ConfigureAwait(true);
                await jsonWriter.WriteEndObjectAsync().ConfigureAwait(true);
            }

            byte[] data = ms.ToArray();

            string json = Encoding.UTF8.GetString(data, 0, data.Length);

            Assert.AreEqual(@"{" + '\n' + @"  ""prop"": true" + '\n' + "}", json);
        }

        [Test]
        public async Task QuoteNameAndStringsAsync()
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);
            JsonTextWriter writer = new JsonTextWriter(sw) { QuoteName = false };

            await writer.WriteStartObjectAsync().ConfigureAwait(true);

            await writer.WritePropertyNameAsync("name").ConfigureAwait(true);
            await writer.WriteValueAsync("value").ConfigureAwait(true);

            await writer.WriteEndObjectAsync().ConfigureAwait(true);
            await writer.FlushAsync().ConfigureAwait(true);

            Assert.AreEqual(@"{name:""value""}", sb.ToString());
        }

        [Test]
        public async Task CloseOutputAsync()
        {
            MemoryStream ms = new MemoryStream();
            JsonTextWriter writer = new JsonTextWriter(new StreamWriter(ms));

            Assert.IsTrue(ms.CanRead);
            await writer.CloseAsync().ConfigureAwait(true);
            Assert.IsFalse(ms.CanRead);

            ms = new MemoryStream();
            writer = new JsonTextWriter(new StreamWriter(ms)) { CloseOutput = false };

            Assert.IsTrue(ms.CanRead);
            await writer.CloseAsync().ConfigureAwait(true);
            Assert.IsTrue(ms.CanRead);
        }

#if !(PORTABLE)
        [Test]
        public async Task WriteIConvertableAsync()
        {
            var sw = new StringWriter();
            JsonTextWriter writer = new JsonTextWriter(sw);
            await writer.WriteValueAsync(new ConvertibleInt(1)).ConfigureAwait(true);

            Assert.AreEqual("1", sw.ToString());
        }
#endif

        [Test]
        public async Task ValueFormattingAsync()
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            using (JsonWriter jsonWriter = new JsonTextWriter(sw))
            {
                await jsonWriter.WriteStartArrayAsync().ConfigureAwait(true);
                await jsonWriter.WriteValueAsync('@').ConfigureAwait(true);
                await jsonWriter.WriteValueAsync("\r\n\t\f\b?{\\r\\n\"\'").ConfigureAwait(true);
                await jsonWriter.WriteValueAsync(true).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync(10).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync(10.99).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync(0.99).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync(0.000000000000000001d).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync(0.000000000000000001m).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync((string)null).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync((object)null).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync("This is a string.").ConfigureAwait(true);
                await jsonWriter.WriteNullAsync().ConfigureAwait(true);
                await jsonWriter.WriteUndefinedAsync().ConfigureAwait(true);
                await jsonWriter.WriteEndArrayAsync().ConfigureAwait(true);
            }

            string expected = @"[""@"",""\r\n\t\f\b?{\\r\\n\""'"",true,10,10.99,0.99,1E-18,0.000000000000000001,null,null,""This is a string."",null,undefined]";
            string result = sb.ToString();

            Assert.AreEqual(expected, result);
        }

        [Test]
        public async Task NullableValueFormattingAsync()
        {
            StringWriter sw = new StringWriter();
            using (JsonTextWriter jsonWriter = new JsonTextWriter(sw))
            {
                await jsonWriter.WriteStartArrayAsync().ConfigureAwait(true);
                await jsonWriter.WriteValueAsync((char?)null).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync((char?)'c').ConfigureAwait(true);
                await jsonWriter.WriteValueAsync((bool?)null).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync((bool?)true).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync((byte?)null).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync((byte?)1).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync((sbyte?)null).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync((sbyte?)1).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync((short?)null).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync((short?)1).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync((ushort?)null).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync((ushort?)1).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync((int?)null).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync((int?)1).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync((uint?)null).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync((uint?)1).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync((long?)null).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync((long?)1).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync((ulong?)null).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync((ulong?)1).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync((double?)null).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync((double?)1.1).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync((float?)null).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync((float?)1.1).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync((decimal?)null).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync((decimal?)1.1m).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync((DateTime?)null).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync((DateTime?)new DateTime(DateTimeUtils.InitialJavaScriptDateTicks, DateTimeKind.Utc)).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync((DateTimeOffset?)null).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync((DateTimeOffset?)new DateTimeOffset(DateTimeUtils.InitialJavaScriptDateTicks, TimeSpan.Zero)).ConfigureAwait(true);
                await jsonWriter.WriteEndArrayAsync().ConfigureAwait(true);
            }

            string json = sw.ToString();
            string expected = @"[null,""c"",null,true,null,1,null,1,null,1,null,1,null,1,null,1,null,1,null,1,null,1.1,null,1.1,null,1.1,null,""1970-01-01T00:00:00Z"",null,""1970-01-01T00:00:00+00:00""]";

            Assert.AreEqual(expected, json);
        }

        [Test]
        public async Task WriteValueObjectWithNullableAsync()
        {
            StringWriter sw = new StringWriter();
            using (JsonTextWriter jsonWriter = new JsonTextWriter(sw))
            {
                char? value = 'c';

                await jsonWriter.WriteStartArrayAsync().ConfigureAwait(true);
                await jsonWriter.WriteValueAsync((object)value).ConfigureAwait(true);
                await jsonWriter.WriteEndArrayAsync().ConfigureAwait(true);
            }

            string json = sw.ToString();
            string expected = @"[""c""]";

            Assert.AreEqual(expected, json);
        }

        [Test]
        public async Task WriteValueObjectWithUnsupportedValueAsync()
        {
            await ExceptionAssert.ThrowsAsync<JsonWriterException>(async () =>
            {
                StringWriter sw = new StringWriter();
                using (JsonTextWriter jsonWriter = new JsonTextWriter(sw))
                {
                    await jsonWriter.WriteStartArrayAsync().ConfigureAwait(true);
                    await jsonWriter.WriteValueAsync(new Version(1, 1, 1, 1)).ConfigureAwait(true);
                    await jsonWriter.WriteEndArrayAsync().ConfigureAwait(true);
                }
            }, @"Unsupported type: System.Version. Use the JsonSerializer class to get the object's JSON representation. Path ''.").ConfigureAwait(true);
        }

        [Test]
        public async Task StringEscapingAsync()
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            using (JsonWriter jsonWriter = new JsonTextWriter(sw))
            {
                await jsonWriter.WriteStartArrayAsync().ConfigureAwait(true);
                await jsonWriter.WriteValueAsync(@"""These pretzels are making me thirsty!""").ConfigureAwait(true);
                await jsonWriter.WriteValueAsync("Jeff's house was burninated.").ConfigureAwait(true);
                await jsonWriter.WriteValueAsync("1. You don't talk about fight club.\r\n2. You don't talk about fight club.").ConfigureAwait(true);
                await jsonWriter.WriteValueAsync("35% of\t statistics\n are made\r up.").ConfigureAwait(true);
                await jsonWriter.WriteEndArrayAsync().ConfigureAwait(true);
            }

            string expected = @"[""\""These pretzels are making me thirsty!\"""",""Jeff's house was burninated."",""1. You don't talk about fight club.\r\n2. You don't talk about fight club."",""35% of\t statistics\n are made\r up.""]";
            string result = sb.ToString();

            Assert.AreEqual(expected, result);
        }

        [Test]
        public async Task WriteEndAsync()
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            using (JsonWriter jsonWriter = new JsonTextWriter(sw))
            {
                jsonWriter.Formatting = Formatting.Indented;

                await jsonWriter.WriteStartObjectAsync().ConfigureAwait(true);
                await jsonWriter.WritePropertyNameAsync("CPU").ConfigureAwait(true);
                await jsonWriter.WriteValueAsync("Intel").ConfigureAwait(true);
                await jsonWriter.WritePropertyNameAsync("PSU").ConfigureAwait(true);
                await jsonWriter.WriteValueAsync("500W").ConfigureAwait(true);
                await jsonWriter.WritePropertyNameAsync("Drives").ConfigureAwait(true);
                await jsonWriter.WriteStartArrayAsync().ConfigureAwait(true);
                await jsonWriter.WriteValueAsync("DVD read/writer").ConfigureAwait(true);
                await jsonWriter.WriteCommentAsync("(broken)").ConfigureAwait(true);
                await jsonWriter.WriteValueAsync("500 gigabyte hard drive").ConfigureAwait(true);
                await jsonWriter.WriteValueAsync("200 gigabype hard drive").ConfigureAwait(true);
                await jsonWriter.WriteEndObjectAsync().ConfigureAwait(true);
                Assert.AreEqual(WriteState.Start, jsonWriter.WriteState);
            }

            string expected = @"{
  ""CPU"": ""Intel"",
  ""PSU"": ""500W"",
  ""Drives"": [
    ""DVD read/writer""
    /*(broken)*/,
    ""500 gigabyte hard drive"",
    ""200 gigabype hard drive""
  ]
}";
            string result = sb.ToString();

            StringAssert.AreEqual(expected, result);
        }

        [Test]
        public async Task CloseWithRemainingContentAsync()
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            using (JsonWriter jsonWriter = new JsonTextWriter(sw))
            {
                jsonWriter.Formatting = Formatting.Indented;

                await jsonWriter.WriteStartObjectAsync().ConfigureAwait(true);
                await jsonWriter.WritePropertyNameAsync("CPU").ConfigureAwait(true);
                await jsonWriter.WriteValueAsync("Intel").ConfigureAwait(true);
                await jsonWriter.WritePropertyNameAsync("PSU").ConfigureAwait(true);
                await jsonWriter.WriteValueAsync("500W").ConfigureAwait(true);
                await jsonWriter.WritePropertyNameAsync("Drives").ConfigureAwait(true);
                await jsonWriter.WriteStartArrayAsync().ConfigureAwait(true);
                await jsonWriter.WriteValueAsync("DVD read/writer").ConfigureAwait(true);
                await jsonWriter.WriteCommentAsync("(broken)").ConfigureAwait(true);
                await jsonWriter.WriteValueAsync("500 gigabyte hard drive").ConfigureAwait(true);
                await jsonWriter.WriteValueAsync("200 gigabype hard drive").ConfigureAwait(true);
                await jsonWriter.CloseAsync().ConfigureAwait(true);
            }

            string expected = @"{
  ""CPU"": ""Intel"",
  ""PSU"": ""500W"",
  ""Drives"": [
    ""DVD read/writer""
    /*(broken)*/,
    ""500 gigabyte hard drive"",
    ""200 gigabype hard drive""
  ]
}";
            string result = sb.ToString();

            StringAssert.AreEqual(expected, result);
        }

        [Test]
        public async Task IndentingAsync()
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            using (JsonWriter jsonWriter = new JsonTextWriter(sw))
            {
                jsonWriter.Formatting = Formatting.Indented;

                await jsonWriter.WriteStartObjectAsync().ConfigureAwait(true);
                await jsonWriter.WritePropertyNameAsync("CPU").ConfigureAwait(true);
                await jsonWriter.WriteValueAsync("Intel").ConfigureAwait(true);
                await jsonWriter.WritePropertyNameAsync("PSU").ConfigureAwait(true);
                await jsonWriter.WriteValueAsync("500W").ConfigureAwait(true);
                await jsonWriter.WritePropertyNameAsync("Drives").ConfigureAwait(true);
                await jsonWriter.WriteStartArrayAsync().ConfigureAwait(true);
                await jsonWriter.WriteValueAsync("DVD read/writer").ConfigureAwait(true);
                await jsonWriter.WriteCommentAsync("(broken)").ConfigureAwait(true);
                await jsonWriter.WriteValueAsync("500 gigabyte hard drive").ConfigureAwait(true);
                await jsonWriter.WriteValueAsync("200 gigabype hard drive").ConfigureAwait(true);
                await jsonWriter.WriteEndAsync().ConfigureAwait(true);
                await jsonWriter.WriteEndObjectAsync().ConfigureAwait(true);
                Assert.AreEqual(WriteState.Start, jsonWriter.WriteState);
            }

            // {
            //   "CPU": "Intel",
            //   "PSU": "500W",
            //   "Drives": [
            //     "DVD read/writer"
            //     /*(broken)*/,
            //     "500 gigabyte hard drive",
            //     "200 gigabype hard drive"
            //   ]
            // }

            string expected = @"{
  ""CPU"": ""Intel"",
  ""PSU"": ""500W"",
  ""Drives"": [
    ""DVD read/writer""
    /*(broken)*/,
    ""500 gigabyte hard drive"",
    ""200 gigabype hard drive""
  ]
}";
            string result = sb.ToString();

            StringAssert.AreEqual(expected, result);
        }

        [Test]
        public async Task StateAsync()
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            using (JsonWriter jsonWriter = new JsonTextWriter(sw))
            {
                Assert.AreEqual(WriteState.Start, jsonWriter.WriteState);

                await jsonWriter.WriteStartObjectAsync().ConfigureAwait(true);
                Assert.AreEqual(WriteState.Object, jsonWriter.WriteState);
                Assert.AreEqual("", jsonWriter.Path);

                await jsonWriter.WritePropertyNameAsync("CPU").ConfigureAwait(true);
                Assert.AreEqual(WriteState.Property, jsonWriter.WriteState);
                Assert.AreEqual("CPU", jsonWriter.Path);

                await jsonWriter.WriteValueAsync("Intel").ConfigureAwait(true);
                Assert.AreEqual(WriteState.Object, jsonWriter.WriteState);
                Assert.AreEqual("CPU", jsonWriter.Path);

                await jsonWriter.WritePropertyNameAsync("Drives").ConfigureAwait(true);
                Assert.AreEqual(WriteState.Property, jsonWriter.WriteState);
                Assert.AreEqual("Drives", jsonWriter.Path);

                await jsonWriter.WriteStartArrayAsync().ConfigureAwait(true);
                Assert.AreEqual(WriteState.Array, jsonWriter.WriteState);

                await jsonWriter.WriteValueAsync("DVD read/writer").ConfigureAwait(true);
                Assert.AreEqual(WriteState.Array, jsonWriter.WriteState);
                Assert.AreEqual("Drives[0]", jsonWriter.Path);

                await jsonWriter.WriteEndAsync().ConfigureAwait(true);
                Assert.AreEqual(WriteState.Object, jsonWriter.WriteState);
                Assert.AreEqual("Drives", jsonWriter.Path);

                await jsonWriter.WriteEndObjectAsync().ConfigureAwait(true);
                Assert.AreEqual(WriteState.Start, jsonWriter.WriteState);
                Assert.AreEqual("", jsonWriter.Path);
            }
        }

        [Test]
        public async Task FloatingPointNonFiniteNumbers_SymbolAsync()
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            using (JsonWriter jsonWriter = new JsonTextWriter(sw))
            {
                jsonWriter.Formatting = Formatting.Indented;
                jsonWriter.FloatFormatHandling = FloatFormatHandling.Symbol;

                await jsonWriter.WriteStartArrayAsync().ConfigureAwait(true);
                await jsonWriter.WriteValueAsync(double.NaN).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync(double.PositiveInfinity).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync(double.NegativeInfinity).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync(float.NaN).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync(float.PositiveInfinity).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync(float.NegativeInfinity).ConfigureAwait(true);
                await jsonWriter.WriteEndArrayAsync().ConfigureAwait(true);

                await jsonWriter.FlushAsync().ConfigureAwait(true);
            }

            string expected = @"[
  NaN,
  Infinity,
  -Infinity,
  NaN,
  Infinity,
  -Infinity
]";
            string result = sb.ToString();

            StringAssert.AreEqual(expected, result);
        }

        [Test]
        public async Task FloatingPointNonFiniteNumbers_ZeroAsync()
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            using (JsonWriter jsonWriter = new JsonTextWriter(sw))
            {
                jsonWriter.Formatting = Formatting.Indented;
                jsonWriter.FloatFormatHandling = FloatFormatHandling.DefaultValue;

                await jsonWriter.WriteStartArrayAsync().ConfigureAwait(true);
                await jsonWriter.WriteValueAsync(double.NaN).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync(double.PositiveInfinity).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync(double.NegativeInfinity).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync(float.NaN).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync(float.PositiveInfinity).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync(float.NegativeInfinity).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync((double?)double.NaN).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync((double?)double.PositiveInfinity).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync((double?)double.NegativeInfinity).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync((float?)float.NaN).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync((float?)float.PositiveInfinity).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync((float?)float.NegativeInfinity).ConfigureAwait(true);
                await jsonWriter.WriteEndArrayAsync().ConfigureAwait(true);

                await jsonWriter.FlushAsync().ConfigureAwait(true);
            }

            string expected = @"[
  0.0,
  0.0,
  0.0,
  0.0,
  0.0,
  0.0,
  null,
  null,
  null,
  null,
  null,
  null
]";
            string result = sb.ToString();

            StringAssert.AreEqual(expected, result);
        }

        [Test]
        public async Task FloatingPointNonFiniteNumbers_StringAsync()
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            using (JsonWriter jsonWriter = new JsonTextWriter(sw))
            {
                jsonWriter.Formatting = Formatting.Indented;
                jsonWriter.FloatFormatHandling = FloatFormatHandling.String;

                await jsonWriter.WriteStartArrayAsync().ConfigureAwait(true);
                await jsonWriter.WriteValueAsync(double.NaN).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync(double.PositiveInfinity).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync(double.NegativeInfinity).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync(float.NaN).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync(float.PositiveInfinity).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync(float.NegativeInfinity).ConfigureAwait(true);
                await jsonWriter.WriteEndArrayAsync().ConfigureAwait(true);

                await jsonWriter.FlushAsync().ConfigureAwait(true);
            }

            string expected = @"[
  ""NaN"",
  ""Infinity"",
  ""-Infinity"",
  ""NaN"",
  ""Infinity"",
  ""-Infinity""
]";
            string result = sb.ToString();

            StringAssert.AreEqual(expected, result);
        }

        [Test]
        public async Task FloatingPointNonFiniteNumbers_QuoteCharAsync()
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            using (JsonTextWriter jsonWriter = new JsonTextWriter(sw))
            {
                jsonWriter.Formatting = Formatting.Indented;
                jsonWriter.FloatFormatHandling = FloatFormatHandling.String;
                jsonWriter.QuoteChar = '\'';

                await jsonWriter.WriteStartArrayAsync().ConfigureAwait(true);
                await jsonWriter.WriteValueAsync(double.NaN).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync(double.PositiveInfinity).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync(double.NegativeInfinity).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync(float.NaN).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync(float.PositiveInfinity).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync(float.NegativeInfinity).ConfigureAwait(true);
                await jsonWriter.WriteEndArrayAsync().ConfigureAwait(true);

                await jsonWriter.FlushAsync().ConfigureAwait(true);
            }

            string expected = @"[
  'NaN',
  'Infinity',
  '-Infinity',
  'NaN',
  'Infinity',
  '-Infinity'
]";
            string result = sb.ToString();

            StringAssert.AreEqual(expected, result);
        }

        [Test]
        public async Task WriteRawInStartAsync()
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            using (JsonWriter jsonWriter = new JsonTextWriter(sw))
            {
                jsonWriter.Formatting = Formatting.Indented;
                jsonWriter.FloatFormatHandling = FloatFormatHandling.Symbol;

                await jsonWriter.WriteRawAsync("[1,2,3,4,5]").ConfigureAwait(true);
                await jsonWriter.WriteWhitespaceAsync("  ").ConfigureAwait(true);
                await jsonWriter.WriteStartArrayAsync().ConfigureAwait(true);
                await jsonWriter.WriteValueAsync(double.NaN).ConfigureAwait(true);
                await jsonWriter.WriteEndArrayAsync().ConfigureAwait(true);
            }

            string expected = @"[1,2,3,4,5]  [
  NaN
]";
            string result = sb.ToString();

            StringAssert.AreEqual(expected, result);
        }

        [Test]
        public async Task WriteRawInArrayAsync()
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            using (JsonWriter jsonWriter = new JsonTextWriter(sw))
            {
                jsonWriter.Formatting = Formatting.Indented;
                jsonWriter.FloatFormatHandling = FloatFormatHandling.Symbol;

                await jsonWriter.WriteStartArrayAsync().ConfigureAwait(true);
                await jsonWriter.WriteValueAsync(double.NaN).ConfigureAwait(true);
                await jsonWriter.WriteRawAsync(",[1,2,3,4,5]").ConfigureAwait(true);
                await jsonWriter.WriteRawAsync(",[1,2,3,4,5]").ConfigureAwait(true);
                await jsonWriter.WriteValueAsync(float.NaN).ConfigureAwait(true);
                await jsonWriter.WriteEndArrayAsync().ConfigureAwait(true);
            }

            string expected = @"[
  NaN,[1,2,3,4,5],[1,2,3,4,5],
  NaN
]";
            string result = sb.ToString();

            StringAssert.AreEqual(expected, result);
        }

        [Test]
        public async Task WriteRawInObjectAsync()
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            using (JsonWriter jsonWriter = new JsonTextWriter(sw))
            {
                jsonWriter.Formatting = Formatting.Indented;

                await jsonWriter.WriteStartObjectAsync().ConfigureAwait(true);
                await jsonWriter.WriteRawAsync(@"""PropertyName"":[1,2,3,4,5]").ConfigureAwait(true);
                await jsonWriter.WriteEndAsync().ConfigureAwait(true);
            }

            string expected = @"{""PropertyName"":[1,2,3,4,5]}";
            string result = sb.ToString();

            Assert.AreEqual(expected, result);
        }

        [Test]
        public async Task WriteTokenAsync()
        {
            CancellationToken cancel = CancellationToken.None;
            JsonTextReader reader = new JsonTextReader(new StringReader("[1,2,3,4,5]"));
            reader.Read();
            reader.Read();

            StringWriter sw = new StringWriter();
            JsonTextWriter writer = new JsonTextWriter(sw);
            await writer.WriteTokenAsync(reader, cancel).ConfigureAwait(true);

            Assert.AreEqual("1", sw.ToString());
        }

        [Test]
        public async Task WriteRawValueAsync()
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            using (JsonWriter jsonWriter = new JsonTextWriter(sw))
            {
                int i = 0;
                string rawJson = "[1,2]";

                await jsonWriter.WriteStartObjectAsync().ConfigureAwait(true);

                while (i < 3)
                {
                    await jsonWriter.WritePropertyNameAsync("d" + i).ConfigureAwait(true);
                    await jsonWriter.WriteRawValueAsync(rawJson).ConfigureAwait(true);

                    i++;
                }

                await jsonWriter.WriteEndObjectAsync().ConfigureAwait(true);
            }

            Assert.AreEqual(@"{""d0"":[1,2],""d1"":[1,2],""d2"":[1,2]}", sb.ToString());
        }

        [Test]
        public async Task WriteObjectNestedInConstructorAsync()
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            using (JsonWriter jsonWriter = new JsonTextWriter(sw))
            {
                await jsonWriter.WriteStartObjectAsync().ConfigureAwait(true);
                await jsonWriter.WritePropertyNameAsync("con").ConfigureAwait(true);

                await jsonWriter.WriteStartConstructorAsync("Ext.data.JsonStore").ConfigureAwait(true);
                await jsonWriter.WriteStartObjectAsync().ConfigureAwait(true);
                await jsonWriter.WritePropertyNameAsync("aa").ConfigureAwait(true);
                await jsonWriter.WriteValueAsync("aa").ConfigureAwait(true);
                await jsonWriter.WriteEndObjectAsync().ConfigureAwait(true);
                await jsonWriter.WriteEndConstructorAsync().ConfigureAwait(true);

                await jsonWriter.WriteEndObjectAsync().ConfigureAwait(true);
            }

            Assert.AreEqual(@"{""con"":new Ext.data.JsonStore({""aa"":""aa""})}", sb.ToString());
        }

        [Test]
        public async Task WriteFloatingPointNumberAsync()
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            using (JsonWriter jsonWriter = new JsonTextWriter(sw))
            {
                jsonWriter.FloatFormatHandling = FloatFormatHandling.Symbol;

                await jsonWriter.WriteStartArrayAsync().ConfigureAwait(true);

                await jsonWriter.WriteValueAsync(0.0).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync(0f).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync(0.1).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync(1.0).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync(1.000001).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync(0.000001).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync(double.Epsilon).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync(double.PositiveInfinity).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync(double.NegativeInfinity).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync(double.NaN).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync(double.MaxValue).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync(double.MinValue).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync(float.PositiveInfinity).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync(float.NegativeInfinity).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync(float.NaN).ConfigureAwait(true);

                await jsonWriter.WriteEndArrayAsync().ConfigureAwait(true);
            }

            Assert.AreEqual(@"[0.0,0.0,0.1,1.0,1.000001,1E-06,4.94065645841247E-324,Infinity,-Infinity,NaN,1.7976931348623157E+308,-1.7976931348623157E+308,Infinity,-Infinity,NaN]", sb.ToString());
        }

        [Test]
        public async Task WriteIntegerNumberAsync()
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            using (JsonWriter jsonWriter = new JsonTextWriter(sw) { Formatting = Formatting.Indented })
            {
                await jsonWriter.WriteStartArrayAsync().ConfigureAwait(true);

                await jsonWriter.WriteValueAsync(int.MaxValue).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync(int.MinValue).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync(0).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync(-0).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync(9L).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync(9UL).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync(long.MaxValue).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync(long.MinValue).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync(ulong.MaxValue).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync(ulong.MinValue).ConfigureAwait(true);

                await jsonWriter.WriteEndArrayAsync().ConfigureAwait(true);
            }

            Console.WriteLine(sb.ToString());

            StringAssert.AreEqual(@"[
  2147483647,
  -2147483648,
  0,
  0,
  9,
  9,
  9223372036854775807,
  -9223372036854775808,
  18446744073709551615,
  0
]", sb.ToString());
        }

        [Test]
        public async Task WriteTokenDirectAsync()
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            using (JsonWriter jsonWriter = new JsonTextWriter(sw))
            {
                await jsonWriter.WriteTokenAsync(JsonToken.StartArray).ConfigureAwait(true);
                await jsonWriter.WriteTokenAsync(JsonToken.Integer, 1).ConfigureAwait(true);
                await jsonWriter.WriteTokenAsync(JsonToken.StartObject).ConfigureAwait(true);
                await jsonWriter.WriteTokenAsync(JsonToken.PropertyName, "string").ConfigureAwait(true);
                await jsonWriter.WriteTokenAsync(JsonToken.Integer, int.MaxValue).ConfigureAwait(true);
                await jsonWriter.WriteTokenAsync(JsonToken.EndObject).ConfigureAwait(true);
                await jsonWriter.WriteTokenAsync(JsonToken.EndArray).ConfigureAwait(true);
            }

            Assert.AreEqual(@"[1,{""string"":2147483647}]", sb.ToString());
        }

        [Test]
        public async Task WriteTokenDirect_BadValueAsync()
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            using (JsonWriter jsonWriter = new JsonTextWriter(sw))
            {
                await jsonWriter.WriteTokenAsync(JsonToken.StartArray).ConfigureAwait(true);

                await ExceptionAssert.ThrowsAsync<FormatException>(async () => { await jsonWriter.WriteTokenAsync(JsonToken.Integer, "three").ConfigureAwait(true); }, "Input string was not in a correct format.").ConfigureAwait(true);

                await ExceptionAssert.ThrowsAsync<ArgumentNullException>(async () => { await jsonWriter.WriteTokenAsync(JsonToken.Integer).ConfigureAwait(true); }, @"Value cannot be null.
Parameter name: value").ConfigureAwait(true);
            }
        }

        [Test]
        public async Task WriteTokenNullCheckAsync()
        {
            using (JsonWriter jsonWriter = new JsonTextWriter(new StringWriter()))
            {
                await ExceptionAssert.ThrowsAsync<ArgumentNullException>(async () => { await jsonWriter.WriteTokenAsync(null).ConfigureAwait(true); }).ConfigureAwait(true);
                await ExceptionAssert.ThrowsAsync<ArgumentNullException>(async () => { await jsonWriter.WriteTokenAsync(null, true).ConfigureAwait(true); }).ConfigureAwait(true);
            }
        }

        [Test]
        public async Task TokenTypeOutOfRangeAsync()
        {
            using (JsonWriter jsonWriter = new JsonTextWriter(new StringWriter()))
            {
                ArgumentOutOfRangeException ex = await ExceptionAssert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await jsonWriter.WriteTokenAsync((JsonToken)int.MinValue)).ConfigureAwait(true);
                Assert.AreEqual("token", ex.ParamName);

                ex = await ExceptionAssert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await jsonWriter.WriteTokenAsync((JsonToken)int.MinValue, "test")).ConfigureAwait(true);
                Assert.AreEqual("token", ex.ParamName);
            }
        }

        [Test]
        public async Task BadWriteEndArrayAsync()
        {
            await ExceptionAssert.ThrowsAsync<JsonWriterException>(async () =>
            {
                StringBuilder sb = new StringBuilder();
                StringWriter sw = new StringWriter(sb);

                using (JsonWriter jsonWriter = new JsonTextWriter(sw))
                {
                    await jsonWriter.WriteStartArrayAsync().ConfigureAwait(true);

                    await jsonWriter.WriteValueAsync(0.0).ConfigureAwait(true);

                    await jsonWriter.WriteEndArrayAsync().ConfigureAwait(true);
                    await jsonWriter.WriteEndArrayAsync().ConfigureAwait(true);
                }
            }, "No token to close. Path ''.").ConfigureAwait(true);
        }

        [Test]
        public async Task IndentationAsync()
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            using (JsonTextWriter jsonWriter = new JsonTextWriter(sw))
            {
                jsonWriter.Formatting = Formatting.Indented;
                jsonWriter.FloatFormatHandling = FloatFormatHandling.Symbol;

                Assert.AreEqual(Formatting.Indented, jsonWriter.Formatting);

                jsonWriter.Indentation = 5;
                Assert.AreEqual(5, jsonWriter.Indentation);
                jsonWriter.IndentChar = '_';
                Assert.AreEqual('_', jsonWriter.IndentChar);
                jsonWriter.QuoteName = true;
                Assert.AreEqual(true, jsonWriter.QuoteName);
                jsonWriter.QuoteChar = '\'';
                Assert.AreEqual('\'', jsonWriter.QuoteChar);

                await jsonWriter.WriteStartObjectAsync().ConfigureAwait(true);

                await jsonWriter.WritePropertyNameAsync("propertyName").ConfigureAwait(true);
                await jsonWriter.WriteValueAsync(double.NaN).ConfigureAwait(true);

                jsonWriter.IndentChar = '?';
                Assert.AreEqual('?', jsonWriter.IndentChar);
                jsonWriter.Indentation = 6;
                Assert.AreEqual(6, jsonWriter.Indentation);

                await jsonWriter.WritePropertyNameAsync("prop2").ConfigureAwait(true);
                await jsonWriter.WriteValueAsync(123).ConfigureAwait(true);

                await jsonWriter.WriteEndObjectAsync().ConfigureAwait(true);
            }

            string expected = @"{
_____'propertyName': NaN,
??????'prop2': 123
}";
            string result = sb.ToString();

            StringAssert.AreEqual(expected, result);
        }

        [Test]
        public async Task WriteSingleBytesAsync()
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            string text = "Hello world.";
            byte[] data = Encoding.UTF8.GetBytes(text);

            using (JsonTextWriter jsonWriter = new JsonTextWriter(sw))
            {
                jsonWriter.Formatting = Formatting.Indented;
                Assert.AreEqual(Formatting.Indented, jsonWriter.Formatting);

                await jsonWriter.WriteValueAsync(data).ConfigureAwait(true);
            }

            string expected = @"""SGVsbG8gd29ybGQu""";
            string result = sb.ToString();

            Assert.AreEqual(expected, result);

            byte[] d2 = Convert.FromBase64String(result.Trim('"'));

            Assert.AreEqual(text, Encoding.UTF8.GetString(d2, 0, d2.Length));
        }

        [Test]
        public async Task WriteBytesInArrayAsync()
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            string text = "Hello world.";
            byte[] data = Encoding.UTF8.GetBytes(text);

            using (JsonTextWriter jsonWriter = new JsonTextWriter(sw))
            {
                jsonWriter.Formatting = Formatting.Indented;
                Assert.AreEqual(Formatting.Indented, jsonWriter.Formatting);

                await jsonWriter.WriteStartArrayAsync().ConfigureAwait(true);
                await jsonWriter.WriteValueAsync(data).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync(data).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync((object)data).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync((byte[])null).ConfigureAwait(true);
                await jsonWriter.WriteValueAsync((Uri)null).ConfigureAwait(true);
                await jsonWriter.WriteEndArrayAsync().ConfigureAwait(true);
            }

            string expected = @"[
  ""SGVsbG8gd29ybGQu"",
  ""SGVsbG8gd29ybGQu"",
  ""SGVsbG8gd29ybGQu"",
  null,
  null
]";
            string result = sb.ToString();

            StringAssert.AreEqual(expected, result);
        }

        [Test]
        public async Task PathAsync()
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            using (JsonTextWriter writer = new JsonTextWriter(sw))
            {
                writer.Formatting = Formatting.Indented;

                await writer.WriteStartArrayAsync().ConfigureAwait(true);
                Assert.AreEqual("", writer.Path);
                await writer.WriteStartObjectAsync().ConfigureAwait(true);
                Assert.AreEqual("[0]", writer.Path);
                await writer.WritePropertyNameAsync("Property1").ConfigureAwait(true);
                Assert.AreEqual("[0].Property1", writer.Path);
                await writer.WriteStartArrayAsync().ConfigureAwait(true);
                Assert.AreEqual("[0].Property1", writer.Path);
                await writer.WriteValueAsync(1).ConfigureAwait(true);
                Assert.AreEqual("[0].Property1[0]", writer.Path);
                await writer.WriteStartArrayAsync().ConfigureAwait(true);
                Assert.AreEqual("[0].Property1[1]", writer.Path);
                await writer.WriteStartArrayAsync().ConfigureAwait(true);
                Assert.AreEqual("[0].Property1[1][0]", writer.Path);
                await writer.WriteStartArrayAsync().ConfigureAwait(true);
                Assert.AreEqual("[0].Property1[1][0][0]", writer.Path);
                await writer.WriteEndObjectAsync().ConfigureAwait(true);
                Assert.AreEqual("[0]", writer.Path);
                await writer.WriteStartObjectAsync().ConfigureAwait(true);
                Assert.AreEqual("[1]", writer.Path);
                await writer.WritePropertyNameAsync("Property2").ConfigureAwait(true);
                Assert.AreEqual("[1].Property2", writer.Path);
                await writer.WriteStartConstructorAsync("Constructor1").ConfigureAwait(true);
                Assert.AreEqual("[1].Property2", writer.Path);
                await writer.WriteNullAsync().ConfigureAwait(true);
                Assert.AreEqual("[1].Property2[0]", writer.Path);
                await writer.WriteStartArrayAsync().ConfigureAwait(true);
                Assert.AreEqual("[1].Property2[1]", writer.Path);
                await writer.WriteValueAsync(1).ConfigureAwait(true);
                Assert.AreEqual("[1].Property2[1][0]", writer.Path);
                await writer.WriteEndAsync().ConfigureAwait(true);
                Assert.AreEqual("[1].Property2[1]", writer.Path);
                await writer.WriteEndObjectAsync().ConfigureAwait(true);
                Assert.AreEqual("[1]", writer.Path);
                await writer.WriteEndArrayAsync().ConfigureAwait(true);
                Assert.AreEqual("", writer.Path);
            }

            StringAssert.AreEqual(@"[
  {
    ""Property1"": [
      1,
      [
        [
          []
        ]
      ]
    ]
  },
  {
    ""Property2"": new Constructor1(
      null,
      [
        1
      ]
    )
  }
]", sb.ToString());
        }

        [Test]
        public async Task DateTimeZoneHandlingAsync()
        {
            StringWriter sw = new StringWriter();
            JsonTextWriter writer = new JsonTextWriter(sw)
            {
                DateTimeZoneHandling = DateTimeZoneHandling.Utc
            };

            await writer.WriteValueAsync(new DateTime(2000, 1, 1, 1, 1, 1, DateTimeKind.Unspecified)).ConfigureAwait(true);

            Assert.AreEqual(@"""2000-01-01T01:01:01Z""", sw.ToString());
        }

        [Test]
        public async Task HtmlStringEscapeHandlingAsync()
        {
            StringWriter sw = new StringWriter();
            JsonTextWriter writer = new JsonTextWriter(sw)
            {
                StringEscapeHandling = StringEscapeHandling.EscapeHtml
            };

            string script = @"<script type=""text/javascript"">alert('hi');</script>";

            await writer.WriteValueAsync(script).ConfigureAwait(true);

            string json = sw.ToString();

            Assert.AreEqual(@"""\u003cscript type=\u0022text/javascript\u0022\u003ealert(\u0027hi\u0027);\u003c/script\u003e""", json);

            JsonTextReader reader = new JsonTextReader(new StringReader(json));

            Assert.AreEqual(script, reader.ReadAsString());
        }

        [Test]
        public async Task NonAsciiStringEscapeHandlingAsync()
        {
            StringWriter sw = new StringWriter();
            JsonTextWriter writer = new JsonTextWriter(sw)
            {
                StringEscapeHandling = StringEscapeHandling.EscapeNonAscii
            };

            string unicode = "\u5f20";

            await writer.WriteValueAsync(unicode).ConfigureAwait(true);

            string json = sw.ToString();

            Assert.AreEqual(8, json.Length);
            Assert.AreEqual(@"""\u5f20""", json);

            JsonTextReader reader = new JsonTextReader(new StringReader(json));

            Assert.AreEqual(unicode, reader.ReadAsString());

            sw = new StringWriter();
            writer = new JsonTextWriter(sw)
            {
                StringEscapeHandling = StringEscapeHandling.Default
            };

            await writer.WriteValueAsync(unicode).ConfigureAwait(true);

            json = sw.ToString();

            Assert.AreEqual(3, json.Length);
            Assert.AreEqual("\"\u5f20\"", json);
        }

        [Test]
        public async Task WriteEndOnPropertyAsync()
        {
            StringWriter sw = new StringWriter();
            JsonTextWriter writer = new JsonTextWriter(sw);
            writer.QuoteChar = '\'';

            await writer.WriteStartObjectAsync().ConfigureAwait(true);
            await writer.WritePropertyNameAsync("Blah").ConfigureAwait(true);
            await writer.WriteEndAsync().ConfigureAwait(true);

            Assert.AreEqual("{'Blah':null}", sw.ToString());
        }

        [Test]
        public async Task QuoteCharAsync()
        {
            StringWriter sw = new StringWriter();
            JsonTextWriter writer = new JsonTextWriter(sw);
            writer.Formatting = Formatting.Indented;
            writer.QuoteChar = '\'';

            await writer.WriteStartArrayAsync().ConfigureAwait(true);

            await writer.WriteValueAsync(new DateTime(2000, 1, 1, 1, 1, 1, DateTimeKind.Utc)).ConfigureAwait(true);
            await writer.WriteValueAsync(new DateTimeOffset(2000, 1, 1, 1, 1, 1, TimeSpan.Zero)).ConfigureAwait(true);

            writer.DateFormatHandling = DateFormatHandling.MicrosoftDateFormat;
            await writer.WriteValueAsync(new DateTime(2000, 1, 1, 1, 1, 1, DateTimeKind.Utc)).ConfigureAwait(true);
            await writer.WriteValueAsync(new DateTimeOffset(2000, 1, 1, 1, 1, 1, TimeSpan.Zero)).ConfigureAwait(true);

            writer.DateFormatString = "yyyy gg";
            await writer.WriteValueAsync(new DateTime(2000, 1, 1, 1, 1, 1, DateTimeKind.Utc)).ConfigureAwait(true);
            await writer.WriteValueAsync(new DateTimeOffset(2000, 1, 1, 1, 1, 1, TimeSpan.Zero)).ConfigureAwait(true);

            await writer.WriteValueAsync(new byte[] { 1, 2, 3 }).ConfigureAwait(true);
            await writer.WriteValueAsync(TimeSpan.Zero).ConfigureAwait(true);
            await writer.WriteValueAsync(new Uri("http://www.google.com/")).ConfigureAwait(true);
            await writer.WriteValueAsync(Guid.Empty).ConfigureAwait(true);

            await writer.WriteEndAsync().ConfigureAwait(true);

            StringAssert.AreEqual(@"[
  '2000-01-01T01:01:01Z',
  '2000-01-01T01:01:01+00:00',
  '\/Date(946688461000)\/',
  '\/Date(946688461000+0000)\/',
  '2000 A.D.',
  '2000 A.D.',
  'AQID',
  '00:00:00',
  'http://www.google.com/',
  '00000000-0000-0000-0000-000000000000'
]", sw.ToString());
        }

        [Test]
        public async Task CultureAsync()
        {
            CultureInfo culture = new CultureInfo("en-NZ");
            culture.DateTimeFormat.AMDesignator = "a.m.";
            culture.DateTimeFormat.PMDesignator = "p.m.";

            StringWriter sw = new StringWriter();
            JsonTextWriter writer = new JsonTextWriter(sw);
            writer.Formatting = Formatting.Indented;
            writer.DateFormatString = "yyyy tt";
            writer.Culture = culture;
            writer.QuoteChar = '\'';

            await writer.WriteStartArrayAsync().ConfigureAwait(true);

            await writer.WriteValueAsync(new DateTime(2000, 1, 1, 1, 1, 1, DateTimeKind.Utc)).ConfigureAwait(true);
            await writer.WriteValueAsync(new DateTimeOffset(2000, 1, 1, 1, 1, 1, TimeSpan.Zero)).ConfigureAwait(true);

            await writer.WriteEndAsync().ConfigureAwait(true);

            StringAssert.AreEqual(@"[
  '2000 a.m.',
  '2000 a.m.'
]", sw.ToString());
        }

        [Test]
        public async Task CompareNewStringEscapingWithOldAsync()
        {
            char c = (char)0;

            do
            {
                StringWriter swNew = new StringWriter();
                char[] buffer = null;
                buffer = await JavaScriptUtils.WriteEscapedJavaScriptStringAsync(swNew, c.ToString(), '"', true, JavaScriptUtils.DoubleQuoteCharEscapeFlags, StringEscapeHandling.Default, null, buffer).ConfigureAwait(true);

                StringWriter swOld = new StringWriter();
                WriteEscapedJavaScriptStringOld(swOld, c.ToString(), '"', true);

                string newText = swNew.ToString();
                string oldText = swOld.ToString();

                if (newText != oldText)
                {
                    throw new Exception("Difference for char '{0}' (value {1}). Old text: {2}, New text: {3}".FormatWith(CultureInfo.InvariantCulture, c, (int)c, oldText, newText));
                }

                c++;
            } while (c != char.MaxValue);
        }

        private const string EscapedUnicodeText = "!";

        private static void WriteEscapedJavaScriptStringOld(TextWriter writer, string s, char delimiter, bool appendDelimiters)
        {
            // leading delimiter
            if (appendDelimiters)
            {
                writer.Write(delimiter);
            }

            if (s != null)
            {
                char[] chars = null;
                char[] unicodeBuffer = null;
                int lastWritePosition = 0;

                for (int i = 0; i < s.Length; i++)
                {
                    var c = s[i];

                    // don't escape standard text/numbers except '\' and the text delimiter
                    if (c >= ' ' && c < 128 && c != '\\' && c != delimiter)
                    {
                        continue;
                    }

                    string escapedValue;

                    switch (c)
                    {
                        case '\t':
                            escapedValue = @"\t";
                            break;
                        case '\n':
                            escapedValue = @"\n";
                            break;
                        case '\r':
                            escapedValue = @"\r";
                            break;
                        case '\f':
                            escapedValue = @"\f";
                            break;
                        case '\b':
                            escapedValue = @"\b";
                            break;
                        case '\\':
                            escapedValue = @"\\";
                            break;
                        case '\u0085': // Next Line
                            escapedValue = @"\u0085";
                            break;
                        case '\u2028': // Line Separator
                            escapedValue = @"\u2028";
                            break;
                        case '\u2029': // Paragraph Separator
                            escapedValue = @"\u2029";
                            break;
                        case '\'':
                            // this charater is being used as the delimiter
                            escapedValue = @"\'";
                            break;
                        case '"':
                            // this charater is being used as the delimiter
                            escapedValue = "\\\"";
                            break;
                        default:
                            if (c <= '\u001f')
                            {
                                if (unicodeBuffer == null)
                                {
                                    unicodeBuffer = new char[6];
                                }

                                StringUtils.ToCharAsUnicode(c, unicodeBuffer);

                                // slightly hacky but it saves multiple conditions in if test
                                escapedValue = EscapedUnicodeText;
                            }
                            else
                            {
                                escapedValue = null;
                            }
                            break;
                    }

                    if (escapedValue == null)
                    {
                        continue;
                    }

                    if (i > lastWritePosition)
                    {
                        if (chars == null)
                        {
                            chars = s.ToCharArray();
                        }

                        // write unchanged chars before writing escaped text
                        writer.Write(chars, lastWritePosition, i - lastWritePosition);
                    }

                    lastWritePosition = i + 1;
                    if (!string.Equals(escapedValue, EscapedUnicodeText))
                    {
                        writer.Write(escapedValue);
                    }
                    else
                    {
                        writer.Write(unicodeBuffer);
                    }
                }

                if (lastWritePosition == 0)
                {
                    // no escaped text, write entire string
                    writer.Write(s);
                }
                else
                {
                    if (chars == null)
                    {
                        chars = s.ToCharArray();
                    }

                    // write remaining text
                    writer.Write(chars, lastWritePosition, s.Length - lastWritePosition);
                }
            }

            // trailing delimiter
            if (appendDelimiters)
            {
                writer.Write(delimiter);
            }
        }

        [Test]
        public async Task CustomJsonTextWriterTestsAsync()
        {
            StringWriter sw = new StringWriter();
            CustomJsonTextWriter writer = new CustomAsyncJsonTextWriter(sw) { Formatting = Formatting.Indented };
            await writer.WriteStartObjectAsync().ConfigureAwait(true);
            Assert.AreEqual(WriteState.Object, writer.WriteState);
            await writer.WritePropertyNameAsync("Property1").ConfigureAwait(true);
            Assert.AreEqual(WriteState.Property, writer.WriteState);
            Assert.AreEqual("Property1", writer.Path);
            await writer.WriteNullAsync().ConfigureAwait(true);
            Assert.AreEqual(WriteState.Object, writer.WriteState);
            await writer.WriteEndObjectAsync().ConfigureAwait(true);
            Assert.AreEqual(WriteState.Start, writer.WriteState);

            StringAssert.AreEqual(@"{{{
  ""1ytreporP"": NULL!!!
}}}", sw.ToString());
        }

        [Test]
        public async Task QuoteDictionaryNamesAsync()
        {
            var d = new Dictionary<string, int>
            {
                { "a", 1 },
            };
            var jsonSerializerSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
            };
            var serializer = JsonSerializer.Create(jsonSerializerSettings);
            using (var stringWriter = new StringWriter())
            {
                using (var writer = new JsonTextWriter(stringWriter) { QuoteName = false })
                {
                    serializer.Serialize(writer, d);
                    await writer.CloseAsync().ConfigureAwait(true);
                }

                StringAssert.AreEqual(@"{
  a: 1
}", stringWriter.ToString());
            }
        }

        [Test]
        public async Task WriteCommentsAsync()
        {
            string json = @"//comment*//*hi*/
{//comment
Name://comment
true//comment after true" + StringUtils.CarriageReturn + @"
,//comment after comma" + StringUtils.CarriageReturnLineFeed + @"
""ExpiryDate""://comment" + StringUtils.LineFeed + @"
new
" + StringUtils.LineFeed +
                          @"Constructor
(//comment
null//comment
),
        ""Price"": 3.99,
        ""Sizes"": //comment
[//comment

          ""Small""//comment
]//comment
}//comment 
//comment 1 ";

            JsonTextReader r = new JsonTextReader(new StringReader(json));

            StringWriter sw = new StringWriter();
            JsonTextWriter w = new JsonTextWriter(sw);
            w.Formatting = Formatting.Indented;

            await w.WriteTokenAsync(r, true).ConfigureAwait(true);

            StringAssert.AreEqual(@"/*comment*//*hi*/*/{/*comment*/
  ""Name"": /*comment*/ true/*comment after true*//*comment after comma*/,
  ""ExpiryDate"": /*comment*/ new Constructor(
    /*comment*/,
    null
    /*comment*/
  ),
  ""Price"": 3.99,
  ""Sizes"": /*comment*/ [
    /*comment*/
    ""Small""
    /*comment*/
  ]/*comment*/
}/*comment *//*comment 1 */", sw.ToString());
        }

        [Test]
        public void AsyncMethodsAlreadyCancelled()
        {
            CancellationTokenSource source = new CancellationTokenSource();
            CancellationToken token = source.Token;
            source.Cancel();

            var writer = new JsonTextWriter(new StreamWriter(Stream.Null));

            Assert.IsTrue(writer.CloseAsync(token).IsCanceled);
            Assert.IsTrue(writer.FlushAsync(token).IsCanceled);
            Assert.IsTrue(writer.WriteCommentAsync("test", token).IsCanceled);
            Assert.IsTrue(writer.WriteEndArrayAsync(token).IsCanceled);
            Assert.IsTrue(writer.WriteEndAsync(token).IsCanceled);
            Assert.IsTrue(writer.WriteEndConstructorAsync(token).IsCanceled);
            Assert.IsTrue(writer.WriteEndObjectAsync(token).IsCanceled);
            Assert.IsTrue(writer.WriteNullAsync(token).IsCanceled);
            Assert.IsTrue(writer.WritePropertyNameAsync("test", token).IsCanceled);
            Assert.IsTrue(writer.WritePropertyNameAsync("test", false, token).IsCanceled);
            Assert.IsTrue(writer.WriteRawAsync("{}", token).IsCanceled);
            Assert.IsTrue(writer.WriteRawValueAsync("{}", token).IsCanceled);
            Assert.IsTrue(writer.WriteStartArrayAsync(token).IsCanceled);
            Assert.IsTrue(writer.WriteStartConstructorAsync("test", token).IsCanceled);
            Assert.IsTrue(writer.WriteStartObjectAsync(token).IsCanceled);
            Assert.IsTrue(writer.WriteTokenAsync(JsonToken.Comment, token).IsCanceled);
            Assert.IsTrue(writer.WriteTokenAsync(JsonToken.Boolean, true, token).IsCanceled);
            JsonTextReader reader = new JsonTextReader(new StringReader("[1,2,3,4,5]"));
            Assert.IsTrue(writer.WriteTokenAsync(reader, token).IsCanceled);
            Assert.IsTrue(writer.WriteUndefinedAsync(token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(bool), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(bool?), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(byte), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(byte?), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(byte[]), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(char), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(char?), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(DateTime), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(DateTime?), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(DateTimeOffset), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(DateTimeOffset?), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(decimal), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(decimal?), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(double), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(double?), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(float), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(float?), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(Guid), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(Guid?), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(int), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(int?), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(long), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(long?), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(object), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(sbyte), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(sbyte?), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(short), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(short?), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(TimeSpan), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(TimeSpan?), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(uint), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(uint?), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(ulong), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(ulong?), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(Uri), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(ushort), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(ushort?), token).IsCanceled);
            Assert.IsTrue(writer.WriteWhitespaceAsync(" ", token).IsCanceled);
        }

        private class NoOverridesDerivedJsonTextWriter : JsonTextWriter
        {
            public NoOverridesDerivedJsonTextWriter(TextWriter textWriter) : base(textWriter)
            {
            }
        }

        private class MinimalOverridesDerivedJsonWriter : JsonWriter
        {
            public override void Flush()
            {
            }
        }

        [Test]
        public void AsyncMethodsAlreadyCancelledOnTextWriterSubclass()
        {
            CancellationTokenSource source = new CancellationTokenSource();
            CancellationToken token = source.Token;
            source.Cancel();

            var writer = new NoOverridesDerivedJsonTextWriter(new StreamWriter(Stream.Null));

            Assert.IsTrue(writer.CloseAsync(token).IsCanceled);
            Assert.IsTrue(writer.FlushAsync(token).IsCanceled);
            Assert.IsTrue(writer.WriteCommentAsync("test", token).IsCanceled);
            Assert.IsTrue(writer.WriteEndArrayAsync(token).IsCanceled);
            Assert.IsTrue(writer.WriteEndAsync(token).IsCanceled);
            Assert.IsTrue(writer.WriteEndConstructorAsync(token).IsCanceled);
            Assert.IsTrue(writer.WriteEndObjectAsync(token).IsCanceled);
            Assert.IsTrue(writer.WriteNullAsync(token).IsCanceled);
            Assert.IsTrue(writer.WritePropertyNameAsync("test", token).IsCanceled);
            Assert.IsTrue(writer.WritePropertyNameAsync("test", false, token).IsCanceled);
            Assert.IsTrue(writer.WriteRawAsync("{}", token).IsCanceled);
            Assert.IsTrue(writer.WriteRawValueAsync("{}", token).IsCanceled);
            Assert.IsTrue(writer.WriteStartArrayAsync(token).IsCanceled);
            Assert.IsTrue(writer.WriteStartConstructorAsync("test", token).IsCanceled);
            Assert.IsTrue(writer.WriteStartObjectAsync(token).IsCanceled);
            Assert.IsTrue(writer.WriteTokenAsync(JsonToken.Comment, token).IsCanceled);
            Assert.IsTrue(writer.WriteTokenAsync(JsonToken.Boolean, true, token).IsCanceled);
            JsonTextReader reader = new JsonTextReader(new StringReader("[1,2,3,4,5]"));
            Assert.IsTrue(writer.WriteTokenAsync(reader, token).IsCanceled);
            Assert.IsTrue(writer.WriteUndefinedAsync(token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(bool), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(bool?), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(byte), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(byte?), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(byte[]), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(char), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(char?), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(DateTime), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(DateTime?), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(DateTimeOffset), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(DateTimeOffset?), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(decimal), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(decimal?), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(double), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(double?), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(float), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(float?), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(Guid), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(Guid?), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(int), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(int?), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(long), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(long?), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(object), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(sbyte), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(sbyte?), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(short), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(short?), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(TimeSpan), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(TimeSpan?), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(uint), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(uint?), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(ulong), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(ulong?), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(Uri), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(ushort), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(ushort?), token).IsCanceled);
            Assert.IsTrue(writer.WriteWhitespaceAsync(" ", token).IsCanceled);
        }

        [Test]
        public void AsyncMethodsAlreadyCancelledOnWriterSubclass()
        {
            CancellationTokenSource source = new CancellationTokenSource();
            CancellationToken token = source.Token;
            source.Cancel();

            var writer = new MinimalOverridesDerivedJsonWriter();

            Assert.IsTrue(writer.CloseAsync(token).IsCanceled);
            Assert.IsTrue(writer.FlushAsync(token).IsCanceled);
            Assert.IsTrue(writer.WriteCommentAsync("test", token).IsCanceled);
            Assert.IsTrue(writer.WriteEndArrayAsync(token).IsCanceled);
            Assert.IsTrue(writer.WriteEndAsync(token).IsCanceled);
            Assert.IsTrue(writer.WriteEndConstructorAsync(token).IsCanceled);
            Assert.IsTrue(writer.WriteEndObjectAsync(token).IsCanceled);
            Assert.IsTrue(writer.WriteNullAsync(token).IsCanceled);
            Assert.IsTrue(writer.WritePropertyNameAsync("test", token).IsCanceled);
            Assert.IsTrue(writer.WritePropertyNameAsync("test", false, token).IsCanceled);
            Assert.IsTrue(writer.WriteRawAsync("{}", token).IsCanceled);
            Assert.IsTrue(writer.WriteRawValueAsync("{}", token).IsCanceled);
            Assert.IsTrue(writer.WriteStartArrayAsync(token).IsCanceled);
            Assert.IsTrue(writer.WriteStartConstructorAsync("test", token).IsCanceled);
            Assert.IsTrue(writer.WriteStartObjectAsync(token).IsCanceled);
            Assert.IsTrue(writer.WriteTokenAsync(JsonToken.Comment, token).IsCanceled);
            Assert.IsTrue(writer.WriteTokenAsync(JsonToken.Boolean, true, token).IsCanceled);
            JsonTextReader reader = new JsonTextReader(new StringReader("[1,2,3,4,5]"));
            Assert.IsTrue(writer.WriteTokenAsync(reader, token).IsCanceled);
            Assert.IsTrue(writer.WriteUndefinedAsync(token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(bool), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(bool?), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(byte), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(byte?), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(byte[]), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(char), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(char?), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(DateTime), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(DateTime?), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(DateTimeOffset), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(DateTimeOffset?), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(decimal), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(decimal?), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(double), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(double?), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(float), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(float?), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(Guid), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(Guid?), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(int), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(int?), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(long), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(long?), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(object), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(sbyte), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(sbyte?), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(short), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(short?), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(TimeSpan), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(TimeSpan?), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(uint), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(uint?), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(ulong), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(ulong?), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(Uri), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(ushort), token).IsCanceled);
            Assert.IsTrue(writer.WriteValueAsync(default(ushort?), token).IsCanceled);
            Assert.IsTrue(writer.WriteWhitespaceAsync(" ", token).IsCanceled);
        }
    }

    public class CustomAsyncJsonTextWriter : CustomJsonTextWriter
    {
        public CustomAsyncJsonTextWriter(TextWriter textWriter) : base(textWriter)
        {
        }

        public override Task WritePropertyNameAsync(string name, CancellationToken cancellationToken = default(CancellationToken))
        {
            return WritePropertyNameAsync(name, true, cancellationToken);
        }

        public override async Task WritePropertyNameAsync(string name, bool escape, CancellationToken cancellationToken = default(CancellationToken))
        {
            await SetWriteStateAsync(JsonToken.PropertyName, name, cancellationToken).ConfigureAwait(true);

            if (QuoteName)
            {
                await _writer.WriteAsync(QuoteChar).ConfigureAwait(true);
            }

            await _writer.WriteAsync(new string(name.ToCharArray().Reverse().ToArray())).ConfigureAwait(true);

            if (QuoteName)
            {
                await _writer.WriteAsync(QuoteChar).ConfigureAwait(true);
            }

            await _writer.WriteAsync(':').ConfigureAwait(true);
        }

        public override async Task WriteNullAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            await SetWriteStateAsync(JsonToken.Null, null, cancellationToken).ConfigureAwait(true);

            await _writer.WriteAsync("NULL!!!").ConfigureAwait(true);
        }

        public override async Task WriteStartObjectAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            await SetWriteStateAsync(JsonToken.StartObject, null, cancellationToken).ConfigureAwait(true);

            await _writer.WriteAsync("{{{").ConfigureAwait(true);
        }

        public override Task WriteEndObjectAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return SetWriteStateAsync(JsonToken.EndObject, null, cancellationToken);
        }

        protected override Task WriteEndAsync(JsonToken token, CancellationToken cancellationToken)
        {
            if (token == JsonToken.EndObject)
            {
                return _writer.WriteAsync("}}}");
            }
            else
            {
                return base.WriteEndAsync(token, cancellationToken);
            }
        }
    }
}
#endif