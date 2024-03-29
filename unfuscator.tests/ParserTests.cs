﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Unfuscator.Core;

namespace Unfuscator.Tests
{
    [TestClass]
    public class ParserTests
    {
        [TestMethod]
        public void ParseStackFrameMethodSignature()
        {
            Assert.AreEqual("System.Decimal.op_Division(Decimal, Decimal)", Signature.ParseStackTraceLine("at System.Decimal.op_Division(Decimal d1, Decimal d2)").ToString());
            Assert.AreEqual("System.Decimal.FCallDivide(Decimal&, Decimal, Decimal)", Signature.ParseStackTraceLine("at System.Decimal.FCallDivide(Decimal& result, Decimal d1, Decimal d2)").ToString());
            Assert.AreEqual("System.Collections.Generic.List`1.Enumerator.MoveNextRare()", Signature.ParseStackTraceLine("at System.Collections.Generic.List`1.Enumerator.MoveNextRare()").ToString()); 
        }

       [TestMethod]
       public void ParseNonEnglishStackTraceLine()
       {
          Assert.AreEqual("System.Decimal.op_Division(Decimal, Decimal)", Signature.ParseStackTraceLine("à System.Decimal.op_Division(Decimal d1, Decimal d2)").ToString());          
       }


      [TestMethod]
        public void ParseDotfuscatorSignature()
        {
            Assert.AreEqual("X(String)", Signature.ParseDotfuscator("string(string)", "X").ToString());
            Assert.AreEqual("X(int[])", Signature.ParseDotfuscator("string(int[])", "X").ToString());
            Assert.AreEqual("X(int[][])", Signature.ParseDotfuscator("string(int[][])", "X").ToString());
            Assert.AreEqual("X(int[,])", Signature.ParseDotfuscator("string(int[0...,0...])", "X").ToString());
            Assert.AreEqual("X(String, ICollection`1)", Signature.ParseDotfuscator("string(string, System.Collections.Generic.ICollection`1<string>)", "X").ToString());
            Assert.AreEqual("X(UInt64)", Signature.ParseDotfuscator("string(unsigned int64)", "X").ToString());
            Assert.AreEqual("X(UInt32)", Signature.ParseDotfuscator("string(unsigned int32)", "X").ToString());
            Assert.AreEqual("X(UInt16)", Signature.ParseDotfuscator("string(unsigned int16)", "X").ToString());
            Assert.AreEqual("X(Byte)", Signature.ParseDotfuscator("string(unsigned int8)", "X").ToString());
            Assert.AreEqual("X(Int64)", Signature.ParseDotfuscator("string(int64)", "X").ToString());
            Assert.AreEqual("X(Int32)", Signature.ParseDotfuscator("string(int32)", "X").ToString());
            Assert.AreEqual("X(Int16)", Signature.ParseDotfuscator("string(int16)", "X").ToString());
            Assert.AreEqual("X(SByte)", Signature.ParseDotfuscator("string(int8)", "X").ToString());
            Assert.AreEqual("X(UInt64)", Signature.ParseDotfuscator("string(native unsigned int)", "X").ToString());
            Assert.AreEqual("X(UInt64)", Signature.ParseDotfuscator("string(unsigned native int)", "X").ToString());
            Assert.AreEqual("X(Int64)", Signature.ParseDotfuscator("string(native int)", "X").ToString());
      }
    }
}
