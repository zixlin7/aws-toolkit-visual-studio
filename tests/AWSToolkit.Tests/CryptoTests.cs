using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;


using Amazon.AWSToolkit;

namespace Amazon.AWSToolkit.Tests
{
    /// <summary>
    /// Summary description for CryptoTests
    /// </summary>
    [TestClass]
    public class CryptoTests
    {
        private const string RSA_PRIVATE_KEY =
@"-----BEGIN RSA PRIVATE KEY-----
MIIEowIBAAKCAQEAkRdzDyiNyKQRCzgiMF20aOKW61i9SV9xkC2EdEl2xbkzOiUvRxKssKS8zHsx
EzJgRa9V/Q0PZj+CLPD9NblR0r5z31fwYLw8GhteDfgRcNzfUV7c99ZcFnlnl00mrD8L4vPTgKYn
bU+P65bVgweoAZgLcVFC60l2G2RI/63ziqTcTGGMaJxlFm1L1QUg7fMUpxXF9hyWKEm2sh/Slh89
Bfd0BU1H7RVOiSy2R3avJqeIWrbmWR0PZlSUyhvk4d8H64Rf8GBlLShe9z91MujSYoflDVs209pY
8to+CA8cjVG35Y/aBfMKtdDEIdj/3t0uCFljNeId+xtt0HhuXkXCnQIDAQABAoIBAQCJ3iBk7QIc
/1l6sbI7By9g4r7Jjx7+U4UTnUzZOt1zcFHvFFpiTKpvh3onS4AMX7f/P2aT+A8D96D4l13j6N6J
RJPSTDuLkBIENLEg0PGxrw88wMlzbus+J8p5iMQQtC/VTh9RhZC9W/bDxCXKRkIskY9989uEu99Q
k/CRk3dx61taBuFhrVMLaqLoBR/C9b1bXdM1+LHiDWYcZjvGGP96eHcbISb5VCfWj3tGx3ZtizwO
l4104AsSWUi1Mp546DB8Y3BAyLOiksZguTL5eirzISB20fkN8xZp0yetzOsCPb/Oed4D/SJgAcez
GucSE6jZwf0eI+ubJqGn0pQvGP35AoGBAMSzQUfFHKIdAGFF6WdC6thtvXCzIYn9ElslWKZM1N6e
ZEorjZ36wl87QgCXA1jtS3vwH24FqCoUaTykt1fN7cVq1OO85QbworZ5qSfc+fgurzOZv7te0AdQ
HuR7BQTGHeZuPjkwlP5BSqdtlIPhzr9vGCxffz4bzrzdwJeWQAxzAoGBALzVMWvL5dK9lK70/eoH
KKCn9qxIASgpJbaOnCwxyiT4QtbDcSuqw/7GU61eJ/zoQwXwOJdP24k8KcR/jhav20kcToP0CgNu
Lf6CJMkxeSHCVkHRMYmdhgWD4KERXZ9RGjUUY1xKHHh76AaiJw7bgjs0/tpgFnReZfg+vlWJfcCv
AoGAOlvEnnqIyEA6gKGxYgWkj5nffrRm2v3OmGQ4LP7WmUX9E1Rgq+JeEMsQBgTH5XZh0t+nM4lS
H0n2/xsPmmlqhgvwJbFBchGq9OCbo0wYjd2r9W9ER18V8VWAFOG613PAI0HKDEWxrs3ITGxih85S
/NEFJwUeR1sQt1BDd7YIQqkCgYBiVKYimw+3WM9m996NEmM+nZhfCDPKBPtFgCek/9xiugCcMzPo
aEkdj4sdWU17bjsQiZH+gTAx22lokH+eIr8O6DWekuLv/FzpDj43opKQWNFv/o5MOgIDNzQuy4s0
HhiGkXJYKaN/vg4J/kBWhUngqO0ZLDYlLM7uoUWd5zXbswKBgB4uUiUKACnS+0p4ACzvMHhiWr47
rx52JL8QteqSc4DVr6s7KSNqf+NBZw6MWdnUsl0ny831oC1b7/VwEBpuFryECionYjBmNl2Kfziw
3z76c3rxMTR2jXFBMnlOuUsIHZHS1IeAZw5zLMMk/XsKnoiYRPD+BbWZJD5MRrjxK8kw
-----END RSA PRIVATE KEY-----";
        
        public CryptoTests()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        [TestMethod]
        public void RoundTripEncryption()
        {
            Convert.ToString(1, 16);
            string originalValue = "this is a basic test";

            string encrypted = UserCrypto.Encrypt(originalValue);
            string decrypted = UserCrypto.Decrypt(encrypted);

            Assert.AreEqual(originalValue, decrypted);
        }

        [TestMethod]
        public void RoundTripRSAPassword()
        {
            string encrypted = UserCrypto.Encrypt(RSA_PRIVATE_KEY);
            string decrypted = UserCrypto.Decrypt(encrypted);

            Assert.AreEqual(RSA_PRIVATE_KEY, decrypted);

        }
    }
}
