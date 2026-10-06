using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tausend.Core.Security;

namespace UnitTesting
{
    // Day 4 AM: the shared-secret check gating the backend<->relay boundary. Exercises
    // SystemAuth.KeysMatch directly -- the fully pure form of the comparison, independent of
    // both WebOperationContext (only populated during a live WCF request) and
    // ConfigurationManager (process-wide, not safely mutable from a test) -- so this is
    // self-contained: no DB, no config file, no WCF host.
    [TestClass]
    public class SystemAuthTesting
    {
        private const string TestKey = "unit-test-relay-system-key-0123456789";

        [TestMethod]
        public void MatchingKeyIsValid()
        {
            Assert.IsTrue(SystemAuth.KeysMatch(TestKey, TestKey));
        }

        [TestMethod]
        public void WrongKeyIsRejected()
        {
            Assert.IsFalse(SystemAuth.KeysMatch(TestKey, "a-completely-different-key"));
        }

        [TestMethod]
        public void KeyOfDifferentLengthIsRejected()
        {
            Assert.IsFalse(SystemAuth.KeysMatch(TestKey, TestKey.Substring(0, TestKey.Length - 1)));
        }

        [TestMethod]
        public void MissingIncomingKeyIsRejected()
        {
            Assert.IsFalse(SystemAuth.KeysMatch(TestKey, null));
            Assert.IsFalse(SystemAuth.KeysMatch(TestKey, ""));
        }

        [TestMethod]
        public void UnconfiguredServerKeyRejectsEverything()
        {
            // A blank/missing RelaySystemKey must fail closed, not accept every request.
            Assert.IsFalse(SystemAuth.KeysMatch("", TestKey));
            Assert.IsFalse(SystemAuth.KeysMatch(null, TestKey));
            Assert.IsFalse(SystemAuth.KeysMatch("", ""));
        }

        [TestMethod]
        public void ComparisonIsCaseSensitive()
        {
            Assert.IsFalse(SystemAuth.KeysMatch(TestKey, TestKey.ToUpperInvariant()));
        }
    }
}
