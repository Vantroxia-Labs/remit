using Microsoft.AspNetCore.Http;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
//using System.Security.Cryptography;

namespace AegisEInvoicing.Infrastructure.Services.Utilitties;

public static class Utility
{
    public static string ToTitleCase(this string stringValue)
    {
        TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
        return textInfo.ToTitleCase(stringValue);
    }

    public static string RandomString(int maxLength)
    {
        Guid guid = Guid.NewGuid();
        string randString = Convert.ToBase64String(guid.ToByteArray());
        randString = DateTime.Now.Millisecond + randString.Replace("=", "").Replace("+", "").Replace("/", "") + DateTime.Now.Millisecond;
        return randString.Substring(0, maxLength);
    }

    public static DateTime StringToDate(this string stringValue)
    {
        CultureInfo provider = CultureInfo.InvariantCulture;
        DateTime date = DateTime.ParseExact(stringValue, new string[] { "MM.dd.yyyy", "MM-dd-yyyy", "MM/dd/yyyy", "dd-MM-yyyy", "dd/MM/yyyy", "dd-MMM-yyyy", "yyyy-MM-dd" }, provider, DateTimeStyles.None);
        return date.Date;
    }

    public static string CRLFRemoval(this string token)
    {
        return token.Replace("%0d", string.Empty, StringComparison.InvariantCultureIgnoreCase).Replace("%0D", string.Empty, StringComparison.InvariantCultureIgnoreCase).Replace("\r", string.Empty).Replace("\n", string.Empty).Replace("%0a", string.Empty, StringComparison.InvariantCultureIgnoreCase).Replace("%0A", string.Empty, StringComparison.InvariantCultureIgnoreCase);
    }   

    public static string MaskPhoneNumber(string number)
    {
        if (string.IsNullOrEmpty(number)) return string.Empty;
        return number.Replace(number.Substring(3, 5), "****");
    }

    public static bool IsValidPhoneNumber(this string phoneNumber)
    {
        bool isPhoneNo = Regex.IsMatch(phoneNumber, @"(\d{1,2})?\-?\d{10}", RegexOptions.IgnoreCase);
        return isPhoneNo;
    }

    public static string FormatPhoneNumber(this string phoneNumber)
    {
        string newPhone;

        if (phoneNumber.StartsWith("+234"))
        {
            newPhone = phoneNumber.Substring(4); // Remove +234
            phoneNumber = "0" + newPhone;
        }
        else if (phoneNumber.StartsWith("234"))
        {
            newPhone = phoneNumber.Substring(3); // Remove 234
            phoneNumber = "0" + newPhone;
        }
        else if (phoneNumber.StartsWith("7") || phoneNumber.StartsWith("8") || phoneNumber.StartsWith("9"))
            return "0" + phoneNumber;

        return phoneNumber;
    }

    public static bool IsValidEmailAddress(this string email)
    {
        bool isEmailValid = Regex.IsMatch(email, @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", RegexOptions.IgnoreCase);
        return isEmailValid;
    }

    public static bool IsDOBGreaterThanToday(this string stringValue)
    {
        bool result = false;

        var birthDate = stringValue.StringToDate();

        if (birthDate.Date <= DateTime.Now.Date)
            result = true;

        return result;
    }

    public static bool IsValidString(this string stringValue)
    {
        bool isNumber = Regex.IsMatch(stringValue, @"^\d+$", RegexOptions.IgnoreCase);
        return isNumber;
    }

    public static bool IsDigit(this string data)
    {
        bool result = false;

        if (data.All(char.IsDigit))
            result = true;

        return result;
    }

    public static string FileToBase64String(IFormFile formFile)
    {
        string base64FileString = string.Empty;

        if (formFile.Length > 0)
        {
            using (var ms = new MemoryStream())
            {
                formFile.CopyTo(ms);
                var fileBytes = ms.ToArray();
                base64FileString = Convert.ToBase64String(fileBytes);
            }
        }

        return base64FileString;
    }
}
