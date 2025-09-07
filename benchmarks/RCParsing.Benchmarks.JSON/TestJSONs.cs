using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCParsing.Benchmarks.JSON
{
	public static class TestJSONs
	{
		public static readonly string shortJson =
		"""
		{
			"id": 1,
			"name": "Sample Data",
			"created": "2023-01-01T00:00:00",
			"tags": ["tag1", "tag2", "tag3"],
			"isActive": true,
			"nested": {
				"value": 123,
				"description": "Nested description"
			}
		}
		""";

		public static readonly string bigJson =
		"""
		{
			"metadata": {
			"generatedAt": "2023-11-15T14:30:00Z",
			"version": 3,
			"tags": ["benchmark", "test", "large"],
			"active": true
			},
			"users": [
			{
				"id": 1001,
				"name": "John Smith",
				"email": "john.smith@example.com",
				"isActive": true,
				"roles": ["admin", "user"],
				"preferences": {
				"theme": "dark",
				"notifications": false,
				"language": "en"
				},
				"lastLogin": "2023-11-14T09:15:22Z"
			},
			{
				"id": 1002,
				"name": "Alice Johnson",
				"email": "alice.j@example.org",
				"isActive": true,
				"roles": ["user"],
				"preferences": {
				"theme": "light",
				"notifications": true,
				"language": "fr"
				},
				"lastLogin": "2023-11-15T08:45:10Z"
			},
			{
				"id": 1003,
				"name": "Bob Brown",
				"email": "bob.brown@example.net",
				"isActive": false,
				"roles": ["guest"],
				"preferences": {
				"theme": "system",
				"notifications": true,
				"language": "de"
				},
				"lastLogin": "2023-10-28T16:20:05Z"
			}
			],
			"products": [
			{
				"sku": "P100",
				"name": "Wireless Keyboard",
				"category": "electronics",
				"price": 5999,
				"stock": 45,
				"specs": {
				"color": "black",
				"wireless": true,
				"batteryLife": 36
				}
			},
			{
				"sku": "P101",
				"name": "Office Chair",
				"category": "furniture",
				"price": 12999,
				"stock": 12,
				"specs": {
				"color": "gray",
				"adjustableHeight": true,
				"material": "mesh"
				}
			},
			{
				"sku": "P102",
				"name": "Notebook",
				"category": "stationery",
				"price": 299,
				"stock": 230,
				"specs": {
				"pages": 120,
				"size": "A5",
				"ruled": true
				}
			}
			],
			"orders": [
			{
				"orderId": 5001,
				"userId": 1001,
				"items": [
				{
				    "sku": "P100",
				    "quantity": 1,
				    "unitPrice": 5999
				},
				{
				    "sku": "P102",
				    "quantity": 3,
				    "unitPrice": 299
				}
				],
				"total": 6896,
				"status": "completed",
				"date": "2023-11-10T11:30:15Z"
			},
			{
				"orderId": 5002,
				"userId": 1002,
				"items": [
				{
				    "sku": "P101",
				    "quantity": 1,
				    "unitPrice": 12999
				}
				],
				"total": 12999,
				"status": "shipped",
				"date": "2023-11-14T14:22:08Z"
			}
			],
			"stats": {
			"totalUsers": 3,
			"activeUsers": 2,
			"totalProducts": 3,
			"totalOrders": 2,
			"revenue": 19895,
			"popularCategories": ["electronics", "furniture"]
			},
			"nested": {
			"level1": {
				"level2": {
				"level3": {
				    "level4": {
				    "level5": {
				        "message": "Deeply nested structure for testing",
				        "flag": false,
				        "count": 5
				    }
				    }
				}
				}
			},
			"arrayLevels": [
				[
				[1, 2],
				[3, 4]
				],
				[
				[5, 6],
				[7, 8]
				]
			]
			},
			"largeArray": [
			{"id": 1, "value": "item1"},
			{"id": 2, "value": "item2"},
			{"id": 3, "value": "item3"},
			{"id": 4, "value": "item4"},
			{"id": 5, "value": "item5"},
			{"id": 6, "value": "item6"},
			{"id": 7, "value": "item7"},
			{"id": 8, "value": "item8"},
			{"id": 9, "value": "item9"},
			{"id": 10, "value": "item10"},
			{"id": 11, "value": "item11"},
			{"id": 12, "value": "item12"},
			{"id": 13, "value": "item13"},
			{"id": 14, "value": "item14"},
			{"id": 15, "value": "item15"},
			{"id": 16, "value": "item16"},
			{"id": 17, "value": "item17"},
			{"id": 18, "value": "item18"},
			{"id": 19, "value": "item19"},
			{"id": 20, "value": "item20"}
			],
			"specialChars": {
			"emptyString": "",
			"escapedChars": "Line1\\nLine2\\tTabbed",
			"unicode": "日本語のテキスト",
			"mixed": "Hello 世界! 123"
			}
		}
		""";
	}
}