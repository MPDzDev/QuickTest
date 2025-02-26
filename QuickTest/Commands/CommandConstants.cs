using System;

namespace QuickTest.Commands
{
    internal static class CommandConstants
    {
        public static readonly Guid CommandSet = new Guid("78bb50c8-df43-4880-bb05-458fd449d8f5");

        public const int UnitTestCommandId = 0x0100;
        public const int IntegrationTestCommandId = 0x0101;
        public const int OriginalClassCommandId = 0x0102;
    }
}
