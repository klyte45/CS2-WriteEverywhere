namespace BelzontWE
{
    public struct WETypeMathOperationDesc
    {
        public enum WEFormulaeMathOperation
        {
            ADD,
            SUBTRACT,
            MULTIPLY,
            DIVIDE
        }
        public enum EnforceType
        {
            None,
            Float,
            Double,
        }

        public readonly string WEDescType => "MATH_OPERATION";
#pragma warning disable IDE1006
        public readonly bool supportsMathOp => true;
#pragma warning restore IDE1006
        public WEFormulaeMathOperation operation;
        public string value;
        public bool isDecimalResult;
        public EnforceType enforceType;

        public static WETypeMathOperationDesc From(WEFormulaeMathOperation operation, float value, EnforceType enforceType, bool isDecimalType) => new()
        {
            operation = operation,
            isDecimalResult = isDecimalType,
            value = value.ToString(),
            enforceType = enforceType
        };

    }

}