using Moq;
using System.Collections.Generic;
using System.Linq;

namespace CaseManager.Tests.Helpers
{
    internal static class MockHelpers
    {
        public static IEnumerable<T> CollectionMatcher<T>(IEnumerable<T> expectation)
        {
            return Match.Create((IEnumerable<T> inputCollection) =>
                                !expectation.Except(inputCollection).Any() &&
                                !inputCollection.Except(expectation).Any());
        }
    }
}
