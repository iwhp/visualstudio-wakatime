// Guids.cs
// MUST match guids.h

namespace Weebree.VsEventLog
{
    using System;

    static class GuidList
    {
        public const string guidWeebree_VsEventLogPkgString = "ee71ab0c-ae8e-4a45-b0b8-af2de89e1b5b";
        public const string guidWeebree_VsEventLogCmdSetString = "f27007cf-e605-40b4-bdfe-3ad03c17735a";
        public const string guidToolWindowPersistanceString = "d89607ce-dac5-4690-942f-263abde8d598";

        public static readonly Guid guidWeebree_VsEventLogCmdSet = new Guid(guidWeebree_VsEventLogCmdSetString);
    };
}