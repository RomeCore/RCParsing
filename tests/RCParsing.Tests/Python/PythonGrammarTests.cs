using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCParsing.Tests.Python
{
	public class PythonGrammarTests
	{
		private Parser parser = PythonParser.CreateParser(b => b
			.Settings.RecordWalkTrace().WriteStackTrace().SetMaxStepsToDisplay(200));
		private Parser optParser = PythonParser.CreateParser(b => b
			.Settings.UseFirstCharacterMatch().UseInlining().IgnoreErrors());

		[Fact]
		public void SimpleParsing()
		{
			var input =
			"""
			def hello_world():
				print("Hello, world!")

			""";

			parser.Parse(input);
			optParser.Parse(input);
		}

		[Fact]
		public void ImportStmt()
		{
			var input =
			"""
			import os, sys
			from math import sqrt, pi

			""";

			parser.Parse(input);
			optParser.Parse(input);
		}

		[Fact]
		public void ClassDefinition()
		{
			var input =
			"""
			class Point:
			    def __init__(self, x, y):
			        self.x = x
			        self.y = y

			    def distance(self, other):
			        return ((self.x - other.x) ** 2 + (self.y - other.y) ** 2) ** 0.5

			""";

			parser.Parse(input);
			optParser.Parse(input);
		}

		[Fact]
		public void KWArgsAndVarArgs()
		{
			var input =
			"""
			def foo(a, b=1, *args, **kwargs):
				return a + b + sum(args) + sum(kwargs.values())

			foo(1, 2, 3, x=4, y=5)
			foo(1, a, b, x=4, y=5, z=6)

			""";

			parser.Parse(input);
			optParser.Parse(input);
		}

		[Fact]
		public void ListComprehension()
		{
			var input =
			"""
			squares = [x**2 for x in range(10)]
			even_squares = [x**2 for x in range(10) if x % 2 == 0]

			""";

			parser.Parse(input);
			optParser.Parse(input);
		}

		[Fact]
		public void LambdaFunction()
		{
			var input =
			"""
			f = lambda x: x + 1
			g = lambda x, y: x * y

			""";

			parser.Parse(input);
			optParser.Parse(input);
		}

		[Fact]
		public void Decorator()
		{
			var input =
			"""
			@staticmethod
			def my_method():
			    pass

			@log_call
			@cache_result
			def expensive_function():
			    pass

			""";

			parser.Parse(input);
			optParser.Parse(input);
		}

		[Fact]
		public void TryExcept()
		{
			var input =
			"""
			try:
			    risky_operation()
			except ValueError as e:
			    handle_value_error(e)
			except (TypeError, RuntimeError):
			    handle_other_errors()
			finally:
			    cleanup()

			""";

			parser.Parse(input);
			optParser.Parse(input);
		}

		[Fact]
		public void AsyncAwait()
		{
			var input =
			"""
			async def fetch_data():
			    result = await some_async_operation()
			    return result

			async def main():
			    tasks = [fetch_data() for _ in range(5)]
			    results = await asyncio.gather(*tasks)
			    return results

			""";

			parser.Parse(input);
			optParser.Parse(input);
		}

		[Fact]
		public void WalrusOperator()
		{
			var input =
			"""
			if (n := len(my_list)) > 10:
			    print(f"List is too long: {n} elements")

			while (line := file.readline()) != "":
			    process(line)

			""";

			parser.Parse(input);
			optParser.Parse(input);
		}

		[Fact]
		public void TypeAnnotations()
		{
			var input =
			"""
			def greet(name: str) -> str:
			    return f"Hello, {name}!"

			class Container:
			    items: list[int]

			    def __init__(self, items: list[int]) -> None:
			        self.items = items

			""";

			parser.Parse(input);
			optParser.Parse(input);
		}

		[Fact]
		public void StdLib_antigravity_py()
		{
			var input =
			"""
			import webbrowser
			import hashlib

			webbrowser.open("https://xkcd.com/353/")

			def geohash(latitude, longitude, datedow):
			    '''Compute geohash() using the Munroe algorithm.

			    >>> geohash(37.421542, -122.085589, b'2005-05-26-10458.68')
			    37.857713 -122.544543

			    '''
			    # https://xkcd.com/426/
			    h = hashlib.md5(datedow, usedforsecurity=False).hexdigest()
			    p, q = [('%f' % float.fromhex('0.' + x)) for x in (h[:16], h[16:32])]
			    print('%d%s %d%s' % (latitude, p[1:], longitude, q[1:]))

			""";

			parser.Parse(input);
			optParser.Parse(input);
		}

		[Fact]
		public void StdLib_bisect_py()
		{
			var input =
			""""
			"""Bisection algorithms."""


			def insort_right(a, x, lo=0, hi=None, *, key=None):
			    """Insert item x in list a, and keep it sorted assuming a is sorted.

			    If x is already in a, insert it to the right of the rightmost x.

			    Optional args lo (default 0) and hi (default len(a)) bound the
			    slice of a to be searched.

			    A custom key function can be supplied to customize the sort order.
			    """
			    if key is None:
			        lo = bisect_right(a, x, lo, hi)
			    else:
			        lo = bisect_right(a, key(x), lo, hi, key=key)
			    a.insert(lo, x)


			def bisect_right(a, x, lo=0, hi=None, *, key=None):
			    """Return the index where to insert item x in list a, assuming a is sorted.

			    The return value i is such that all e in a[:i] have e <= x, and all e in
			    a[i:] have e > x.  So if x already appears in the list, a.insert(i, x) will
			    insert just after the rightmost x already there.

			    Optional args lo (default 0) and hi (default len(a)) bound the
			    slice of a to be searched.

			    A custom key function can be supplied to customize the sort order.
			    """

			    if lo < 0:
			        raise ValueError('lo must be non-negative')
			    if hi is None:
			        hi = len(a)
			    # Note, the comparison uses "<" to match the
			    # __lt__() logic in list.sort() and in heapq.
			    if key is None:
			        while lo < hi:
			            mid = (lo + hi) // 2
			            if x < a[mid]:
			                hi = mid
			            else:
			                lo = mid + 1
			    else:
			        while lo < hi:
			            mid = (lo + hi) // 2
			            if x < key(a[mid]):
			                hi = mid
			            else:
			                lo = mid + 1
			    return lo


			def insort_left(a, x, lo=0, hi=None, *, key=None):
			    """Insert item x in list a, and keep it sorted assuming a is sorted.

			    If x is already in a, insert it to the left of the leftmost x.

			    Optional args lo (default 0) and hi (default len(a)) bound the
			    slice of a to be searched.

			    A custom key function can be supplied to customize the sort order.
			    """

			    if key is None:
			        lo = bisect_left(a, x, lo, hi)
			    else:
			        lo = bisect_left(a, key(x), lo, hi, key=key)
			    a.insert(lo, x)

			def bisect_left(a, x, lo=0, hi=None, *, key=None):
			    """Return the index where to insert item x in list a, assuming a is sorted.

			    The return value i is such that all e in a[:i] have e < x, and all e in
			    a[i:] have e >= x.  So if x already appears in the list, a.insert(i, x) will
			    insert just before the leftmost x already there.

			    Optional args lo (default 0) and hi (default len(a)) bound the
			    slice of a to be searched.

			    A custom key function can be supplied to customize the sort order.
			    """

			    if lo < 0:
			        raise ValueError('lo must be non-negative')
			    if hi is None:
			        hi = len(a)
			    # Note, the comparison uses "<" to match the
			    # __lt__() logic in list.sort() and in heapq.
			    if key is None:
			        while lo < hi:
			            mid = (lo + hi) // 2
			            if a[mid] < x:
			                lo = mid + 1
			            else:
			                hi = mid
			    else:
			        while lo < hi:
			            mid = (lo + hi) // 2
			            if key(a[mid]) < x:
			                lo = mid + 1
			            else:
			                hi = mid
			    return lo


			# Overwrite above definitions with a fast C implementation
			try:
			    from _bisect import *
			except ImportError:
			    pass

			# Create aliases
			bisect = bisect_right
			insort = insort_right

			"""";

			parser.Parse(input);
			optParser.Parse(input);
		}
	}
}