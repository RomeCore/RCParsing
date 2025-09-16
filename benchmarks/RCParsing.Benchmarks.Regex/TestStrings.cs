using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCParsing.Benchmarks.Regex
{
	public static class TestStrings
	{
		public static string identifiersShort =
		"""
		result = 42; // Simple assignment
		_tempVar = calculateValue(input_data);
		isValid = checkCondition(my_object.value);
		for (int i = 0; i < count; i++) { process(item_array[i]); }
		// TODO: Add more logic here
		return final_result;
		""";

		public static string identifiersBig =
		"""
		// ====== SECTION 1: Variable declarations ======
		var counter = 0;
		string userName = "John_Doe123";
		double PI_APPROXIMATION = 3.14159;
		List<int> prime_Numbers = new List<int> { 2, 3, 5, 7, 11 };
		Dictionary<string, object> config_Values = new Dictionary<string, object>();
		bool is_initialized_flag = false;
	
		// ====== SECTION 2: Function calls with mixed content ======
		result = processData(input_source, options);
		if (isValid && !is_completed) {
			try {
				var temp_result = transformer.applyTransformation(raw_data_set);
				logger.logEvent("Transformation completed", LogLevel.INFO);
			} catch (Exception ex) {
				handleError(ex, " Transformation failed!!! ");
			}
		}
	
		// ====== SECTION 3: More complex patterns ======
		for (int index_1 = 0; index_1 < items.length; index_1++) {
			for (int j = 0; j < subItems.count; j++) {
				var current_item = items[index_1].getSubItem(j);
				if (current_item.isEnabled()) {
					processItem(current_item, getContext());
				}
			}
		}
	
		// ====== SECTION 4: Random garbage and broken syntax ======
		broken line without semicolon
		123invalid_identifier = 456
		if (missing_parenthesis {
			callFunction(param1, param2, ; // Extra comma and semicolon
		}
		validButWeird_ = _123;
		$special_char$ = "ignored";
	
		// ====== SECTION 5: Mixed content with various symbols ======
		result = (a + b) * (c - d) / e % f; // Arithmetic
		flags = FLAG_A | FLAG_B & ~FLAG_C; // Bitwise
		message = "String with \"quotes\" and \\backslashes\\";
		price = 123.45USD; // Should be parsed as identifier + garbage
		timestamp = 2021-01-01T00:00:00Z; // ISO date
	
		// ====== SECTION 6: More valid code ======
		try {
			await initializeAsync();
			await processBatchAsync(data_batch);
		} finally {
			cleanupResources();
			resetState();
		}
		// ====== SECTION 1: Variable declarations ======
		var counter = 0;
		string userName = "John_Doe123";
		double PI_APPROXIMATION = 3.14159;
		List<int> prime_Numbers = new List<int> { 2, 3, 5, 7, 11 };
		Dictionary<string, object> config_Values = new Dictionary<string, object>();
		bool is_initialized_flag = false;
	
		// ====== SECTION 2: Function calls with mixed content ======
		result = processData(input_source, options);
		if (isValid && !is_completed) {
			try {
				var temp_result = transformer.applyTransformation(raw_data_set);
				logger.logEvent("Transformation completed", LogLevel.INFO);
			} catch (Exception ex) {
				handleError(ex, " Transformation failed!!! ");
			}
		}
	
		// ====== SECTION 3: More complex patterns ======
		for (int index_1 = 0; index_1 < items.length; index_1++) {
			for (int j = 0; j < subItems.count; j++) {
				var current_item = items[index_1].getSubItem(j);
				if (current_item.isEnabled()) {
					processItem(current_item, getContext());
				}
			}
		}
	
		// ====== SECTION 4: Random garbage and broken syntax ======
		broken line without semicolon
		123invalid_identifier = 456
		if (missing_parenthesis {
			callFunction(param1, param2, ; // Extra comma and semicolon
		}
		validButWeird_ = _123;
		$special_char$ = "ignored";
	
		// ====== SECTION 5: Mixed content with various symbols ======
		result = (a + b) * (c - d) / e % f; // Arithmetic
		flags = FLAG_A | FLAG_B & ~FLAG_C; // Bitwise
		message = "String with \"quotes\" and \\backslashes\\";
		price = 123.45USD; // Should be parsed as identifier + garbage
		timestamp = 2021-01-01T00:00:00Z; // ISO date
	
		// ====== SECTION 6: More valid code ======
		try {
			await initializeAsync();
			await processBatchAsync(data_batch);
		} finally {
			cleanupResources();
			resetState();
		}
		// ====== SECTION 1: Variable declarations ======
		var counter = 0;
		string userName = "John_Doe123";
		double PI_APPROXIMATION = 3.14159;
		List<int> prime_Numbers = new List<int> { 2, 3, 5, 7, 11 };
		Dictionary<string, object> config_Values = new Dictionary<string, object>();
		bool is_initialized_flag = false;
	
		// ====== SECTION 2: Function calls with mixed content ======
		result = processData(input_source, options);
		if (isValid && !is_completed) {
			try {
				var temp_result = transformer.applyTransformation(raw_data_set);
				logger.logEvent("Transformation completed", LogLevel.INFO);
			} catch (Exception ex) {
				handleError(ex, " Transformation failed!!! ");
			}
		}
	
		// ====== SECTION 3: More complex patterns ======
		for (int index_1 = 0; index_1 < items.length; index_1++) {
			for (int j = 0; j < subItems.count; j++) {
				var current_item = items[index_1].getSubItem(j);
				if (current_item.isEnabled()) {
					processItem(current_item, getContext());
				}
			}
		}
	
		// ====== SECTION 4: Random garbage and broken syntax ======
		broken line without semicolon
		123invalid_identifier = 456
		if (missing_parenthesis {
			callFunction(param1, param2, ; // Extra comma and semicolon
		}
		validButWeird_ = _123;
		$special_char$ = "ignored";
	
		// ====== SECTION 5: Mixed content with various symbols ======
		result = (a + b) * (c - d) / e % f; // Arithmetic
		flags = FLAG_A | FLAG_B & ~FLAG_C; // Bitwise
		message = "String with \"quotes\" and \\backslashes\\";
		price = 123.45USD; // Should be parsed as identifier + garbage
		timestamp = 2021-01-01T00:00:00Z; // ISO date
	
		// ====== SECTION 6: More valid code ======
		try {
			await initializeAsync();
			await processBatchAsync(data_batch);
		} finally {
			cleanupResources();
			resetState();
		}
		// ====== SECTION 1: Variable declarations ======
		var counter = 0;
		string userName = "John_Doe123";
		double PI_APPROXIMATION = 3.14159;
		List<int> prime_Numbers = new List<int> { 2, 3, 5, 7, 11 };
		Dictionary<string, object> config_Values = new Dictionary<string, object>();
		bool is_initialized_flag = false;
	
		// ====== SECTION 2: Function calls with mixed content ======
		result = processData(input_source, options);
		if (isValid && !is_completed) {
			try {
				var temp_result = transformer.applyTransformation(raw_data_set);
				logger.logEvent("Transformation completed", LogLevel.INFO);
			} catch (Exception ex) {
				handleError(ex, " Transformation failed!!! ");
			}
		}
	
		// ====== SECTION 3: More complex patterns ======
		for (int index_1 = 0; index_1 < items.length; index_1++) {
			for (int j = 0; j < subItems.count; j++) {
				var current_item = items[index_1].getSubItem(j);
				if (current_item.isEnabled()) {
					processItem(current_item, getContext());
				}
			}
		}
	
		// ====== SECTION 4: Random garbage and broken syntax ======
		broken line without semicolon
		123invalid_identifier = 456
		if (missing_parenthesis {
			callFunction(param1, param2, ; // Extra comma and semicolon
		}
		validButWeird_ = _123;
		$special_char$ = "ignored";
	
		// ====== SECTION 5: Mixed content with various symbols ======
		result = (a + b) * (c - d) / e % f; // Arithmetic
		flags = FLAG_A | FLAG_B & ~FLAG_C; // Bitwise
		message = "String with \"quotes\" and \\backslashes\\";
		price = 123.45USD; // Should be parsed as identifier + garbage
		timestamp = 2021-01-01T00:00:00Z; // ISO date
	
		// ====== SECTION 6: More valid code ======
		try {
			await initializeAsync();
			await processBatchAsync(data_batch);
		} finally {
			cleanupResources();
			resetState();
		}
		// ====== SECTION 1: Variable declarations ======
		var counter = 0;
		string userName = "John_Doe123";
		double PI_APPROXIMATION = 3.14159;
		List<int> prime_Numbers = new List<int> { 2, 3, 5, 7, 11 };
		Dictionary<string, object> config_Values = new Dictionary<string, object>();
		bool is_initialized_flag = false;
	
		// ====== SECTION 2: Function calls with mixed content ======
		result = processData(input_source, options);
		if (isValid && !is_completed) {
			try {
				var temp_result = transformer.applyTransformation(raw_data_set);
				logger.logEvent("Transformation completed", LogLevel.INFO);
			} catch (Exception ex) {
				handleError(ex, " Transformation failed!!! ");
			}
		}
	
		// ====== SECTION 3: More complex patterns ======
		for (int index_1 = 0; index_1 < items.length; index_1++) {
			for (int j = 0; j < subItems.count; j++) {
				var current_item = items[index_1].getSubItem(j);
				if (current_item.isEnabled()) {
					processItem(current_item, getContext());
				}
			}
		}
	
		// ====== SECTION 4: Random garbage and broken syntax ======
		broken line without semicolon
		123invalid_identifier = 456
		if (missing_parenthesis {
			callFunction(param1, param2, ; // Extra comma and semicolon
		}
		validButWeird_ = _123;
		$special_char$ = "ignored";
	
		// ====== SECTION 5: Mixed content with various symbols ======
		result = (a + b) * (c - d) / e % f; // Arithmetic
		flags = FLAG_A | FLAG_B & ~FLAG_C; // Bitwise
		message = "String with \"quotes\" and \\backslashes\\";
		price = 123.45USD; // Should be parsed as identifier + garbage
		timestamp = 2021-01-01T00:00:00Z; // ISO date
	
		// ====== SECTION 6: More valid code ======
		try {
			await initializeAsync();
			await processBatchAsync(data_batch);
		} finally {
			cleanupResources();
			resetState();
		}
		""";

		public static string emailsShort =
		"""
		Contact us at info@example.com for more details.
		Support: support_team@company.org or help@service.io.
		Invalid emails: user@, @domain.com, user@domain.
		admin@server.local is our internal address.
		""";

		public static string emailsBig =
		"""
		// ====== VALID EMAILS ======
		user123@example.com
		john_doe@company.org
		test.user@service.io
		admin@localhost.local
		sales_team@business.net
		contact_us@website.info
		a.b_c@domain.co.uk
		user2024@mail-server.com
		support_123@my-domain.org
		webmaster@site.com
		
		// ====== INVALID OR GARBAGE ======
		This is not an email: just text here
		Missing @: userexample.com
		Invalid format: user@domain@com
		No domain: user@
		No username: @domain.com
		Missing dot: user@domaincom
		Special chars: user+tag@domain.com (should not match \w+ pattern)
		Multiple dots: user@sub.domain.com (should not match simple pattern)
		With spaces: user name@domain.com
		
		// ====== MORE VALID ONES ======
		service_account@api.gov
		notifications_team@alert.system
		backup_admin@storage.box
		dev_team@git.repo
		billing_department@payments.cc
		monitoring@servers.tech
		
		// ====== MIXED CONTENT ======
		Please contact support@helpdesk.com for assistance.
		Our sales department can be reached at sales@company.biz.
		For bugs reports: bugs@dev.team or issues@tracker.system.
		Invalid entries: @, test@, @test.com, user@test.
		
		// ====== EDGE CASES ======
		short@a.b
		very_long_username_1234567890@long-domain-name.extension
		multiple@emails@in@text.com should only match first part
		email@with-dash.com (should not match due to dash)
		email@with_underscore.org (should match underscore)
		// ====== VALID EMAILS ======
		user123@example.com
		john_doe@company.org
		test.user@service.io
		admin@localhost.local
		sales_team@business.net
		contact_us@website.info
		a.b_c@domain.co.uk
		user2024@mail-server.com
		support_123@my-domain.org
		webmaster@site.com
	
		// ====== INVALID OR GARBAGE ======
		This is not an email: just text here
		Missing @: userexample.com
		Invalid format: user@domain@com
		No domain: user@
		No username: @domain.com
		Missing dot: user@domaincom
		Special chars: user+tag@domain.com (should not match \w+ pattern)
		Multiple dots: user@sub.domain.com (should not match simple pattern)
		With spaces: user name@domain.com
	
		// ====== MORE VALID ONES ======
		service_account@api.gov
		notifications_team@alert.system
		backup_admin@storage.box
		dev_team@git.repo
		billing_department@payments.cc
		monitoring@servers.tech
	
		// ====== MIXED CONTENT ======
		Please contact support@helpdesk.com for assistance.
		Our sales department can be reached at sales@company.biz.
		For bugs reports: bugs@dev.team or issues@tracker.system.
		Invalid entries: @, test@, @test.com, user@test.
	
		// ====== EDGE CASES ======
		short@a.b
		very_long_username_1234567890@long-domain-name.extension
		multiple@emails@in@text.com should only match first part
		email@with-dash.com (should not match due to dash)
		email@with_underscore.org (should match underscore)
		// ====== VALID EMAILS ======
		user123@example.com
		john_doe@company.org
		test.user@service.io
		admin@localhost.local
		sales_team@business.net
		contact_us@website.info
		a.b_c@domain.co.uk
		user2024@mail-server.com
		support_123@my-domain.org
		webmaster@site.com
	
		// ====== INVALID OR GARBAGE ======
		This is not an email: just text here
		Missing @: userexample.com
		Invalid format: user@domain@com
		No domain: user@
		No username: @domain.com
		Missing dot: user@domaincom
		Special chars: user+tag@domain.com (should not match \w+ pattern)
		Multiple dots: user@sub.domain.com (should not match simple pattern)
		With spaces: user name@domain.com
	
		// ====== MORE VALID ONES ======
		service_account@api.gov
		notifications_team@alert.system
		backup_admin@storage.box
		dev_team@git.repo
		billing_department@payments.cc
		monitoring@servers.tech
	
		// ====== MIXED CONTENT ======
		Please contact support@helpdesk.com for assistance.
		Our sales department can be reached at sales@company.biz.
		For bugs reports: bugs@dev.team or issues@tracker.system.
		Invalid entries: @, test@, @test.com, user@test.
	
		// ====== EDGE CASES ======
		short@a.b
		very_long_username_1234567890@long-domain-name.extension
		multiple@emails@in@text.com should only match first part
		email@with-dash.com (should not match due to dash)
		email@with_underscore.org (should match underscore)
		// ====== VALID EMAILS ======
		user123@example.com
		john_doe@company.org
		test.user@service.io
		admin@localhost.local
		sales_team@business.net
		contact_us@website.info
		a.b_c@domain.co.uk
		user2024@mail-server.com
		support_123@my-domain.org
		webmaster@site.com
	
		// ====== INVALID OR GARBAGE ======
		This is not an email: just text here
		Missing @: userexample.com
		Invalid format: user@domain@com
		No domain: user@
		No username: @domain.com
		Missing dot: user@domaincom
		Special chars: user+tag@domain.com (should not match \w+ pattern)
		Multiple dots: user@sub.domain.com (should not match simple pattern)
		With spaces: user name@domain.com
	
		// ====== MORE VALID ONES ======
		service_account@api.gov
		notifications_team@alert.system
		backup_admin@storage.box
		dev_team@git.repo
		billing_department@payments.cc
		monitoring@servers.tech
	
		// ====== MIXED CONTENT ======
		Please contact support@helpdesk.com for assistance.
		Our sales department can be reached at sales@company.biz.
		For bugs reports: bugs@dev.team or issues@tracker.system.
		Invalid entries: @, test@, @test.com, user@test.
	
		// ====== EDGE CASES ======
		short@a.b
		very_long_username_1234567890@long-domain-name.extension
		multiple@emails@in@text.com should only match first part
		email@with-dash.com (should not match due to dash)
		email@with_underscore.org (should match underscore)
		// ====== VALID EMAILS ======
		user123@example.com
		john_doe@company.org
		test.user@service.io
		admin@localhost.local
		sales_team@business.net
		contact_us@website.info
		a.b_c@domain.co.uk
		user2024@mail-server.com
		support_123@my-domain.org
		webmaster@site.com
	
		// ====== INVALID OR GARBAGE ======
		This is not an email: just text here
		Missing @: userexample.com
		Invalid format: user@domain@com
		No domain: user@
		No username: @domain.com
		Missing dot: user@domaincom
		Special chars: user+tag@domain.com (should not match \w+ pattern)
		Multiple dots: user@sub.domain.com (should not match simple pattern)
		With spaces: user name@domain.com
	
		// ====== MORE VALID ONES ======
		service_account@api.gov
		notifications_team@alert.system
		backup_admin@storage.box
		dev_team@git.repo
		billing_department@payments.cc
		monitoring@servers.tech
	
		// ====== MIXED CONTENT ======
		Please contact support@helpdesk.com for assistance.
		Our sales department can be reached at sales@company.biz.
		For bugs reports: bugs@dev.team or issues@tracker.system.
		Invalid entries: @, test@, @test.com, user@test.
	
		// ====== EDGE CASES ======
		short@a.b
		very_long_username_1234567890@long-domain-name.extension
		multiple@emails@in@text.com should only match first part
		email@with-dash.com (should not match due to dash)
		email@with_underscore.org (should match underscore)
		""";
	}
}