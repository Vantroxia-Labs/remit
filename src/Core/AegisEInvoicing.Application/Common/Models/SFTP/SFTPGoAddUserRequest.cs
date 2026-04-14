using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AegisEInvoicing.Application.Common.Models.SFTP
{
    public class SFTPGoAddUserRequest
    {
        public string UserName { get; set; }
        public string UserPassword { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }

        public SFTPGoAddUserRequest()
        {
            // Initialize with empty strings to avoid null references
            UserName = string.Empty;
            UserPassword = string.Empty;
            FirstName = string.Empty;
            LastName = string.Empty;
            Email = string.Empty;
        }
    }

    public class SFTPGoRenameUserRequest
    {
        public string UserName { get; set; }
        public string NewUserName { get; set; }

        public SFTPGoRenameUserRequest()
        {
            UserName = string.Empty;
            NewUserName = string.Empty;
        }
    }
    public class SFTPGoChangePasswordRequest
    {
        public string UserName { get; set; }
        public string OldPassword { get; set; }
        public string NewPassword { get; set; }

        public SFTPGoChangePasswordRequest()
        {
            UserName = string.Empty;
            OldPassword = string.Empty;
            NewPassword = string.Empty;
        }
    }
    /// <summary>
    /// Request for getting log messages
    /// </summary>
    public class LogMessagesRequest
    {
        public ulong? StartMessageId { get; set; }
        public int? MaxMessages { get; set; }
    }
}
