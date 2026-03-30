using System.Text.Json;
using AegisEInvoicing.Interswitch.Helpers;
using AegisEInvoicing.Interswitch.Models;

namespace AegisEInvoicing.Interswitch.Tests;

/// <summary>
/// Test cases for Interswitch response deserialization
/// Validates handling of both success and error responses, including stringified JSON payloads
/// </summary>
public static class InterswitchResponseTests
{
    /// <summary>
    /// Test 1: Success response with direct object (not stringified)
    /// </summary>
    public static void TestSuccessWithDirectObject()
    {
        const string json = @"{
            ""success"": true,
            ""data"": {
                ""code"": 201,
                ""data"": {
                    ""ok"": true
                },
                ""message"": ""Invoice transmitted successfully""
            }
        }";

        var response = InterswitchResponseHelper.DeserializeInterswitchResponse<OkResponse>(json);

        Console.WriteLine("Test 1: Success with Direct Object");
        Console.WriteLine($"✅ IsSuccess: {response?.IsSuccess} (Expected: True)");
        Console.WriteLine($"✅ Code: {response?.Data?.Code} (Expected: 201)");
        Console.WriteLine($"✅ Message: {response?.Data?.Message}");
        Console.WriteLine($"✅ Data.Ok: {response?.Data?.Data?.Ok} (Expected: True)");
        Console.WriteLine($"✅ ErrorMessage: {response?.ErrorMessage ?? "null"} (Expected: null)");
        Console.WriteLine();

        if (response?.IsSuccess == true && response?.Data?.Data?.Ok == true)
        {
            Console.WriteLine("✅ Test 1 PASSED\n");
        }
        else
        {
            Console.WriteLine("❌ Test 1 FAILED\n");
        }
    }

    /// <summary>
    /// Test 2: Success response with stringified JSON data field
    /// </summary>
    public static void TestSuccessWithStringifiedData()
    {
        const string json = "{\"success\":true,\"data\":\"{\\\"code\\\":201,\\\"data\\\":{\\\"ok\\\":true},\\\"message\\\":\\\"Invoice transmitted successfully\\\"}\"}";


        var response = InterswitchResponseHelper.DeserializeInterswitchResponse<OkResponse>(json);

        Console.WriteLine("Test 2: Success with Stringified Data");
        Console.WriteLine($"✅ IsSuccess: {response?.IsSuccess} (Expected: True)");
        Console.WriteLine($"✅ Code: {response?.Data?.Code} (Expected: 201)");
        Console.WriteLine($"✅ Message: {response?.Data?.Message}");
        Console.WriteLine($"✅ Data.Ok: {response?.Data?.Data?.Ok} (Expected: True)");
        Console.WriteLine();

        if (response?.IsSuccess == true && response?.Data?.Data?.Ok == true)
        {
            Console.WriteLine("✅ Test 2 PASSED\n");
        }
        else
        {
            Console.WriteLine("❌ Test 2 FAILED\n");
        }
    }

    /// <summary>
    /// Test 3: Error response with direct object
    /// </summary>
    public static void TestErrorWithDirectObject()
    {
        const string json = @"{
            ""success"": true,
            ""data"": {
                ""code"": 404,
                ""data"": null,
                ""message"": ""error has occurred"",
                ""error"": {
                    ""id"": ""d6cb2642-3891-4e95-af79-be66ddfefffc"",
                    ""handler"": ""transmit_actions"",
                    ""details"": ""unable to transmit this invoice as the corresponding access points are offline"",
                    ""public_message"": ""unable to transmit this invoice as the corresponding access points are offline""
                }
            }
        }";

        var response = InterswitchResponseHelper.DeserializeInterswitchResponse<OkResponse>(json);

        Console.WriteLine("Test 3: Error with Direct Object");
        Console.WriteLine($"✅ IsSuccess: {response?.IsSuccess} (Expected: False)");
        Console.WriteLine($"✅ Code: {response?.Data?.Code} (Expected: 404)");
        Console.WriteLine($"✅ Message: {response?.Data?.Message}");
        Console.WriteLine($"✅ ErrorMessage: {response?.ErrorMessage}");
        Console.WriteLine($"✅ Error.Details: {response?.Error?.Details}");
        Console.WriteLine($"✅ Error.PublicMessage: {response?.Error?.PublicMessage}");
        Console.WriteLine();

        if (response?.IsSuccess == false && response?.Error != null)
        {
            Console.WriteLine("✅ Test 3 PASSED\n");
        }
        else
        {
            Console.WriteLine("❌ Test 3 FAILED\n");
        }
    }

    /// <summary>
    /// Test 4: Error response with stringified JSON data field
    /// </summary>
    public static void TestErrorWithStringifiedData()
    {
        const string json = "{\"success\":true,\"data\":\"{\\\"code\\\":404,\\\"data\\\":null,\\\"message\\\":\\\"error has occurred\\\",\\\"error\\\":{\\\"id\\\":\\\"d6cb2642-3891-4e95-af79-be66ddfefffc\\\",\\\"handler\\\":\\\"transmit_actions\\\",\\\"details\\\":\\\"unable to transmit this invoice as the corresponding access points are offline\\\",\\\"public_message\\\":\\\"unable to transmit this invoice as the corresponding access points are offline\\\"}}\"}";


        var response = InterswitchResponseHelper.DeserializeInterswitchResponse<OkResponse>(json);

        Console.WriteLine("Test 4: Error with Stringified Data");
        Console.WriteLine($"✅ IsSuccess: {response?.IsSuccess} (Expected: False)");
        Console.WriteLine($"✅ Code: {response?.Data?.Code} (Expected: 404)");
        Console.WriteLine($"✅ Message: {response?.Data?.Message}");
        Console.WriteLine($"✅ ErrorMessage: {response?.ErrorMessage}");
        Console.WriteLine($"✅ Error.Details: {response?.Error?.Details}");
        Console.WriteLine($"✅ Error.PublicMessage: {response?.Error?.PublicMessage}");
        Console.WriteLine();

        if (response?.IsSuccess == false && response?.Error != null)
        {
            Console.WriteLine("✅ Test 4 PASSED\n");
        }
        else
        {
            Console.WriteLine("❌ Test 4 FAILED\n");
        }
    }

    /// <summary>
    /// Test 5: Direct response (not wrapped) with error
    /// </summary>
    public static void TestDirectErrorResponse()
    {
        const string json = @"{
            ""code"": 404,
            ""data"": null,
            ""message"": ""Invoice not found"",
            ""error"": {
                ""id"": ""12345"",
                ""handler"": ""lookup_handler"",
                ""details"": ""Invoice with IRN XYZ123 not found"",
                ""public_message"": ""Invoice not found""
            }
        }";

        var response = InterswitchResponseHelper.DeserializeDirectResponse<OkResponse>(json);

        Console.WriteLine("Test 5: Direct Error Response (Not Wrapped)");
        Console.WriteLine($"✅ IsSuccess: {response?.IsSuccess} (Expected: False)");
        Console.WriteLine($"✅ Code: {response?.Code} (Expected: 404)");
        Console.WriteLine($"✅ Message: {response?.Message}");
        Console.WriteLine($"✅ Error.Details: {response?.Error?.Details}");
        Console.WriteLine($"✅ Error.PublicMessage: {response?.Error?.PublicMessage}");
        Console.WriteLine();

        if (response?.IsSuccess == false && response?.Error != null)
        {
            Console.WriteLine("✅ Test 5 PASSED\n");
        }
        else
        {
            Console.WriteLine("❌ Test 5 FAILED\n");
        }
    }

    /// <summary>
    /// Run all tests
    /// </summary>
    public static void RunAllTests()
    {
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("INTERSWITCH RESPONSE DESERIALIZATION TESTS");
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine();

        try
        {
            TestSuccessWithDirectObject();
            TestSuccessWithStringifiedData();
            TestErrorWithDirectObject();
            TestErrorWithStringifiedData();
            TestDirectErrorResponse();

            Console.WriteLine("=".PadRight(80, '='));
            Console.WriteLine("ALL TESTS COMPLETED");
            Console.WriteLine("=".PadRight(80, '='));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Test execution failed: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }
}