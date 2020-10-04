using System;

namespace DaemonMaster.Core.Win32.PInvoke.Kernel32
{
    public static partial class Kernel32
    {
        public struct AccessMask
        {
            //TODO
            [Flags]
            public enum StandartRights
            {
                Delete = 0x00010000,
                ReadControl = 0x00020000,
                WriteDac = 0x00040000,
                WriteOwner = 0x00080000,
                Synchronize = 0x00100000,
                StandardRightsRequired = 0x000F0000,
                StandardRightsRead = 0x00020000,
                StandardRightsWrite = 0x00020000,
                StandardRightsExecute = 0x00020000,
                StandardRightsAll = 0x001F0000
            }

            [Flags]
            public enum SpecialSpecificRights
            {
                SpecificRightsAll = 0x0000FFFF,
                AccessSystemSecurity = 0x01000000,
                MaximumAllowed = 0x02000000
            }

            [Flags]
            public enum DesktopSpecificRights
            {
                DesktopReadobjects = 0x00000001,
                DesktopCreatewindow = 0x00000002,
                DesktopCreatemenu = 0x00000004,
                DesktopHookcontrol = 0x00000008,
                DesktopJournalrecord = 0x00000010,
                DesktopJournalplayback = 0x00000020,
                DesktopEnumerate = 0x00000040,
                DesktopWriteobjects = 0x00000080,
                DesktopSwitchdesktop = 0x00000100
            }


            [Flags]
            public enum WinstaSpecificRights
            {
                WinstaEnumdesktops = 0x00000001,
                WinstaReadattributes = 0x00000002,
                WinstaAccessclipboard = 0x00000004,
                WinstaCreatedesktop = 0x00000008,
                WinstaWriteattributes = 0x00000010,
                WinstaAccessglobalatoms = 0x00000020,
                WinstaExitwindows = 0x00000040,
                WinstaEnumerate = 0x00000100,
                WinstaReadscreen = 0x00000200,
                WinstaAllAccess = 0x0000037F
            }

            [Flags]
            public enum PolicySpecificRights
            {
                PolicyViewLocalInformation = 0x00000001,
                PolicyViewAuditInformation = 0x00000002,
                PolicyGetPrivateInformation = 0x00000004,
                PolicyTrustAdmin = 0x00000008,
                PolicyCreateAccount = 0x00000010,
                PolicyCreateSecret = 0x00000020,
                PolicyCreatePrivilege = 0x00000040,
                PolicySetDefaultQuotaLimits = 0x00000080,
                PolicySetAuditRequirements = 0x00000100,
                PolicyAuditLogAdmin = 0x00000200,
                PolicyServerAdmin = 0x00000400,
                PolicyLookupNames = 0x00000800,
                PolicyNotification = 0x00001000,

                PolicyRead = 0x20006,
                PolicyExecute = 0X20801,
                PolicyWrite = 0X207F8,
                PolicyAllAccess = 0x00F0FFF,
            }

            public enum AccessTokensRights
            {
                StandardRightsRequired = 0x000F0000,
                StandardRightsRead = 0x00020000,
                TokenAssignPrimary = 0x0001,
                TokenDuplicate = 0x0002,
                TokenImpersonate = 0x0004,
                TokenQuery = 0x0008,
                TokenQuerySource = 0x0010,
                TokenAdjustPrivileges = 0x0020,
                TokenAdjustGroups = 0x0040,
                TokenAdjustDefault = 0x0080,
                TokenAdjustSessionId = 0x0100,
                TokenRead = (StandardRightsRead | TokenQuery),
                TokenAllAccess = (StandardRightsRequired | TokenAssignPrimary |
                                                        TokenDuplicate | TokenImpersonate | TokenQuery | TokenQuerySource |
                                                        TokenAdjustPrivileges | TokenAdjustGroups | TokenAdjustDefault |
                                                        TokenAdjustSessionId),
            }
        }
    }
}