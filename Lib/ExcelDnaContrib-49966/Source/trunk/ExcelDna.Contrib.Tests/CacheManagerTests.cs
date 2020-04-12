//using System;
//using ExcelDna.Contrib.Cache;
//using NUnit.Framework;
//using NUnit.Framework.SyntaxHelpers;

//namespace ExcelDna.Contrib.Tests
//{
//    [TestFixture]
//    public class CacheManagerTests
//    {
//        private ICacheManager _cm;

//        [SetUp]
//        public void Initialise()
//        {
//            _cm = new CacheManager();
//        }

//        [Test]
//        public void RegisterObjectInCache()
//        {
//            Assert.That(_cm.Count, Is.EqualTo(0));

//            _cm.Register(new object());
            
//            Assert.That(_cm.Count, Is.EqualTo(1));
//        }

//        [Test]
//        public void RegisterObjectInCacheWithSpecificKey()
//        {
//            string key = "my own key";
//            string result = _cm.Register(new object(), key);

//            Assert.That(result, Is.EqualTo(key));
//        }

//        [Test]
//        public void RetrieveObjectFromCache()
//        {
//            object o = new object();
            
//            string key = _cm.Register(o);

//            object fromCache = _cm.Lookup(key);

//            Assert.That(fromCache, Is.SameAs(o));
//        }

//        [Test]
//        public void ClearCache()
//        {
//            _cm.Register(new object());
            
//            Assert.That(_cm.Count, Is.GreaterThan(0));

//            _cm.Clear();

//            Assert.That(_cm.Count, Is.EqualTo(0));
//        }

//        [Test]
//        public void RemoveObjectFromCache()
//        {
//            string key = _cm.Register(new object());

//            Assert.That(_cm.Count, Is.EqualTo(1));

//            _cm.Remove(key);

//            Assert.That(_cm.Count, Is.EqualTo(0));
//        }

//        [Test]
//        public void RequestInvalidKeyFromCacheFails()
//        {
//            string key = _cm.Register(new object());

//            try
//            {
//                _cm.Lookup(key + "bla");
//                Assert.Fail("An exception should have been thrown for invalid key.");
//            }
//            catch (ApplicationException)
//            {
//                // expected
//            }
//        }

//        [Test]
//        public void QueryContainsKeySuccess()

//        {
//            string key = _cm.Register(new object());

//            Assert.IsTrue(_cm.ContainsKey(key));

//        }

//        [Test]
//        public void QueryContainsKeyFailure()
//        {

//            string key = _cm.Register(new object());
//            if (_cm.ContainsKey(string.Empty))
//            {
//                Assert.Fail("An empty String Key should not exist");
//            }
//            else
//            {
//                //expected
//            }

//        }
//    }
//}
