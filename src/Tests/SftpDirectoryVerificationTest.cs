using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using EInvoiceIntegrator.Infrastructure.Services;
using System;
using System.IO;
using System.Threading.Tasks;

namespace EInvoiceIntegrator.Tests
{
    /// <summary>
    /// Test class to verify SFTP directory creation functionality
    /// This test can be run manually to validate the fixes for user directory creation
    /// </summary>
    public class SftpDirectoryVerificationTest
    {
        private readonly SftpDirectoryService _sftpDirectoryService;
        private readonly string _testRootPath;

        public SftpDirectoryVerificationTest()
        {
            // Setup test configuration
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "SftpConfiguration:FtpRootPath", @"C:\TestFtpRoot" }
                })
                .Build();

            // Setup test logger
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var logger = loggerFactory.CreateLogger<SftpDirectoryService>();

            _sftpDirectoryService = new SftpDirectoryService(configuration, logger);
            _testRootPath = @"C:\TestFtpRoot";
        }

        /// <summary>
        /// Tests the complete directory creation and verification flow
        /// </summary>
        public async Task<bool> TestDirectoryCreationFlow()
        {
            var testUsername = $"testuser_{DateTime.Now:yyyyMMddHHmmss}";
            
            try
            {
                Console.WriteLine($"Testing directory creation for user: {testUsername}");
                Console.WriteLine($"Test root path: {_testRootPath}");

                // Ensure test root directory exists
                if (!Directory.Exists(_testRootPath))
                {
                    Directory.CreateDirectory(_testRootPath);
                    Console.WriteLine($"Created test root directory: {_testRootPath}");
                }

                // Test 1: Create user directories
                Console.WriteLine("\n--- Test 1: Creating user directories ---");
                var createResult = await _sftpDirectoryService.CreateUserDirectoriesAsync(testUsername, _testRootPath);
                Console.WriteLine($"Directory creation result: {createResult}");

                if (!createResult)
                {
                    Console.WriteLine("❌ Failed to create directories");
                    return false;
                }

                // Test 2: Verify directories exist
                Console.WriteLine("\n--- Test 2: Verifying directories exist ---");
                var checkResult = await _sftpDirectoryService.CheckDirectoriesExistAsync(testUsername, _testRootPath);
                Console.WriteLine($"Directory existence check result: {checkResult}");

                if (!checkResult)
                {
                    Console.WriteLine("❌ Directory existence check failed");
                    return false;
                }

                // Test 3: Verify expected directory structure
                Console.WriteLine("\n--- Test 3: Verifying directory structure ---");
                var userRootPath = _sftpDirectoryService.GetUserRootPath(testUsername, _testRootPath);
                var expectedDirectories = new[]
                {
                    userRootPath,
                    Path.Combine(userRootPath, "PROCESSED"),
                    Path.Combine(userRootPath, "NACK"),
                    Path.Combine(userRootPath, "ACK")
                };

                bool allDirectoriesExist = true;
                foreach (var expectedDir in expectedDirectories)
                {
                    var exists = Directory.Exists(expectedDir);
                    Console.WriteLine($"  {expectedDir}: {(exists ? "✅" : "❌")}");
                    if (!exists) allDirectoriesExist = false;
                }

                // Test 4: Clean up test directories
                Console.WriteLine("\n--- Test 4: Cleaning up test directories ---");
                var deleteResult = await _sftpDirectoryService.DeleteUserDirectoriesAsync(testUsername, _testRootPath);
                Console.WriteLine($"Directory deletion result: {deleteResult}");

                // Final verification that directories are gone
                var finalCheckResult = await _sftpDirectoryService.CheckDirectoriesExistAsync(testUsername, _testRootPath);
                Console.WriteLine($"Post-deletion existence check (should be false): {finalCheckResult}");

                if (finalCheckResult)
                {
                    Console.WriteLine("❌ Directories still exist after deletion");
                    return false;
                }

                Console.WriteLine($"\n✅ All tests passed for user: {testUsername}");
                return allDirectoriesExist && deleteResult && !finalCheckResult;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test failed with exception: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Tests path validation functionality
        /// </summary>
        public bool TestPathValidation()
        {
            Console.WriteLine("\n=== Testing Path Validation ===");

            var testCases = new[]
            {
                new { Path = @"C:\TestFtpRoot\validuser", Expected = true, Description = "Valid path within FTP root" },
                new { Path = @"C:\TestFtpRoot\..\invalidpath", Expected = false, Description = "Path traversal attempt" },
                new { Path = @"C:\SomeOtherPath\user", Expected = false, Description = "Path outside FTP root" },
                new { Path = "", Expected = false, Description = "Empty path" },
                new { Path = @"C:\TestFtpRoot\user with spaces", Expected = true, Description = "Path with spaces" }
            };

            bool allTestsPassed = true;

            foreach (var testCase in testCases)
            {
                try
                {
                    var result = _sftpDirectoryService.ValidateDirectoryPath(testCase.Path);
                    var passed = result == testCase.Expected;
                    Console.WriteLine($"  {testCase.Description}: {(passed ? "✅" : "❌")} (Expected: {testCase.Expected}, Got: {result})");
                    
                    if (!passed) allTestsPassed = false;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  {testCase.Description}: ❌ Exception: {ex.Message}");
                    allTestsPassed = false;
                }
            }

            return allTestsPassed;
        }

        /// <summary>
        /// Runs all tests
        /// </summary>
        public static async Task<bool> RunAllTests()
        {
            var tester = new SftpDirectoryVerificationTest();
            
            Console.WriteLine("🧪 Starting SFTP Directory Service Tests");
            Console.WriteLine("=========================================");

            // Run path validation tests
            var pathValidationResult = tester.TestPathValidation();
            
            // Run directory creation flow test
            var directoryFlowResult = await tester.TestDirectoryCreationFlow();

            Console.WriteLine("\n=========================================");
            Console.WriteLine("📊 Test Results Summary");
            Console.WriteLine("=========================================");
            Console.WriteLine($"Path Validation Tests: {(pathValidationResult ? "✅ PASSED" : "❌ FAILED")}");
            Console.WriteLine($"Directory Flow Tests: {(directoryFlowResult ? "✅ PASSED" : "❌ FAILED")}");
            
            bool allTestsPassed = pathValidationResult && directoryFlowResult;
            Console.WriteLine($"Overall Result: {(allTestsPassed ? "✅ ALL TESTS PASSED" : "❌ SOME TESTS FAILED")}");

            return allTestsPassed;
        }
    }

    /// <summary>
    /// Simple console program to run the tests
    /// </summary>
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("SFTP Directory Verification Test");
            Console.WriteLine("This test verifies the fixes for user directory creation issues.");
            Console.WriteLine();

            try
            {
                bool success = await SftpDirectoryVerificationTest.RunAllTests();
                Environment.Exit(success ? 0 : 1);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fatal error: {ex.Message}");
                Environment.Exit(1);
            }
        }
    }
}