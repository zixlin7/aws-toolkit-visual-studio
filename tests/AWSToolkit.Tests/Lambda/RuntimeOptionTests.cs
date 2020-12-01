using Amazon.AWSToolkit.Lambda.Model;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace AWSToolkit.Tests.Lambda
{
    public class RuntimeOptionTests
    {
        private readonly List<RuntimeOption> _runtimeOptions;

        public RuntimeOptionTests()
        {
            // Use reflection to grab all of the statically declared RuntimeOption instances
            _runtimeOptions = typeof(RuntimeOption).GetFields(BindingFlags.Static | BindingFlags.Public)
                .Where(f => f.FieldType == typeof(RuntimeOption))
                .Select(f => f.GetValue(null))
                .OfType<RuntimeOption>()
                .ToList();
        }

        [Fact]
        public void IsCustomRuntime()
        {
            Assert.True(RuntimeOption.PROVIDED.IsCustomRuntime);
            Assert.Empty(_runtimeOptions
                .Where(r => r != RuntimeOption.PROVIDED)
                .Where(r => r.IsCustomRuntime)
            );
        }

        [Fact]
        public void AllOptions()
        {
            Assert.Equal(
                _runtimeOptions.OrderBy(r => r.Value),
                RuntimeOption.ALL_OPTIONS.OrderBy(r => r.Value)
            );
        }
    }
}