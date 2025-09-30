using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCParsing.Tests.C;

namespace RCParsing.Tests
{
	public class CGrammarTests
	{
		[Fact]
		public void SimpleC()
		{
			string input =
			"""
			int func(double) {
				return 0.0;
			}

			void main(int) {
				return 2 + 2;
			}
			""";

			var parser = CParser.CreateParser();
			var ast = parser.Parse(input);
		}

		[Fact]
		public void AdvancedC()
		{
			string input =
			"""
			int* func(double x) {
				return x * func(x, 0.0);
			}

			void main(int argc, const char** argv) {
				int* ptr = func(70);
				return 2 + 2 + argc;
			}
			""";

			var parser = CParser.CreateParser();
			var ast = parser.Parse(input);
		}

		[Fact]
		public void ComplexC()
		{
			string input =
			"""
			struct Point {
				int x;
				int y;
			};
			
			typedef struct Point Point;
			
			Point* create_point(int x, int y) {
				Point* p = malloc(sizeof(Point));
				p->x = x;
				p->y = y;
				return p;
			}
			
			int main() {
				Point* pt = create_point(10, 20);
				printf("Point: %d, %d\n", pt->x, pt->y);
				free(pt);
				return 0;
			}
			""";

			var parser = CParser.CreateParser();
			var ast = parser.Parse(input);
		}
	}
}