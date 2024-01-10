using calledudeBot.Services;
using System;
using System.Collections.Generic;
using System.Net.Http;
using Xunit;

namespace calledudeBotCore.Tests.Utilities;
public class TypeExtensionsTests
{
	[Theory]
	[InlineData(typeof(string), "string")]
	[InlineData(typeof(object), "object")]
	[InlineData(typeof(bool), "bool")]
	[InlineData(typeof(byte), "byte")]
	[InlineData(typeof(char), "char")]
	[InlineData(typeof(decimal), "decimal")]
	[InlineData(typeof(double), "double")]
	[InlineData(typeof(short), "short")]
	[InlineData(typeof(int), "int")]
	[InlineData(typeof(long), "long")]
	[InlineData(typeof(sbyte), "sbyte")]
	[InlineData(typeof(float), "float")]
	[InlineData(typeof(ushort), "ushort")]
	[InlineData(typeof(uint), "uint")]
	[InlineData(typeof(ulong), "ulong")]
	[InlineData(typeof(void), "void")]
	public void SimpleType(Type t, string expectedFriendlyName) => Assert.Equal(expectedFriendlyName, t.GetFriendlyName());

	[Theory]
	[InlineData(typeof(bool*), "bool*")]
	[InlineData(typeof(byte*), "byte*")]
	[InlineData(typeof(char*), "char*")]
	[InlineData(typeof(decimal*), "decimal*")]
	[InlineData(typeof(double*), "double*")]
	[InlineData(typeof(short*), "short*")]
	[InlineData(typeof(int*), "int*")]
	[InlineData(typeof(long*), "long*")]
	[InlineData(typeof(sbyte*), "sbyte*")]
	[InlineData(typeof(float*), "float*")]
	[InlineData(typeof(ushort*), "ushort*")]
	[InlineData(typeof(uint*), "uint*")]
	[InlineData(typeof(ulong*), "ulong*")]
	[InlineData(typeof(void*), "void*")]
	public void PointerType(Type t, string expectedFriendlyName) => Assert.Equal(expectedFriendlyName, t.GetFriendlyName());

	[Theory]
	[InlineData(typeof(string[]), "string[]")]
	[InlineData(typeof(object[]), "object[]")]
	[InlineData(typeof(bool[]), "bool[]")]
	[InlineData(typeof(byte[]), "byte[]")]
	[InlineData(typeof(char[]), "char[]")]
	[InlineData(typeof(decimal[]), "decimal[]")]
	[InlineData(typeof(double[]), "double[]")]
	[InlineData(typeof(short[]), "short[]")]
	[InlineData(typeof(int[]), "int[]")]
	[InlineData(typeof(long[]), "long[]")]
	[InlineData(typeof(sbyte[]), "sbyte[]")]
	[InlineData(typeof(float[]), "float[]")]
	[InlineData(typeof(ushort[]), "ushort[]")]
	[InlineData(typeof(uint[]), "uint[]")]
	[InlineData(typeof(ulong[]), "ulong[]")]
	public void ArrayType(Type t, string expectedFriendlyName) => Assert.Equal(expectedFriendlyName, t.GetFriendlyName());

	[Theory]
	[InlineData(typeof(List<int*[]>), "List<int*[]>")]
	[InlineData(typeof(List<List<int>>), "List<List<int>>")]
	[InlineData(typeof(Dictionary<int, string>), "Dictionary<int, string>")]
	[InlineData(typeof(Dictionary<int, List<KeyValuePair<string, HttpClient>>>), "Dictionary<int, List<KeyValuePair<string, HttpClient>>>")]
	public void GenericType(Type t, string expectedFriendlyName) => Assert.Equal(expectedFriendlyName, t.GetFriendlyName());
}