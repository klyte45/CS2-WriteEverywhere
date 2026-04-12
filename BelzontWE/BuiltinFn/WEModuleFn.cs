using Colossal.IO.AssetDatabase;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Unity.Entities;

namespace BelzontWE.Builtin
{
    [WEBuiltinFunction("Module")]
    public static class WEModuleFn
    {
        private static readonly Dictionary<string, bool> moduleStates = [];

        // Matches: <module name> then one operator/paren or end-of-string, then the rest
        private static readonly Regex tokenRegex = new(@"^([^|&()]+)([|&()]|$)(.*$)", RegexOptions.Compiled);

        private static bool IsModSingleEnabled(string modName)
        {
            modName = modName.Trim();
            if (!moduleStates.TryGetValue(modName, out var isEnabled))
            {
                var asset = AssetDatabase.global.GetAsset(
                    SearchFilter<ExecutableAsset>.ByCondition(a => a.isMod && a.isLoaded && a.name == modName));
                isEnabled = asset != null && asset.isMod;
                moduleStates[modName] = isEnabled;
            }
            return isEnabled;
        }

        /// <summary>
        /// Evaluates an expression that may contain module names joined by '&' (AND), '|' (OR) and grouped by parentheses.
        /// Operator precedence: '&' binds tighter than '|'.
        /// </summary>
        private static bool IsModuleEnabled(string expression)
        {
            if (!moduleStates.TryGetValue(expression, out var cached))
            {
                cached = EvaluateOr(expression.Trim(), out _);
                moduleStates[expression] = cached;
            }
            return cached;
        }

        // OR is the lowest precedence: split on '|' at the top level
        private static bool EvaluateOr(string expr, out string remaining)
        {
            var result = EvaluateAnd(expr, out remaining);
            while (remaining.Length > 0 && remaining[0] == '|')
            {
                if (result) return true;
                var rhs = EvaluateAnd(remaining.Substring(1), out remaining);
                result = result || rhs;
            }
            return result;
        }

        // AND is higher precedence: split on '&' before handling OR
        private static bool EvaluateAnd(string expr, out string remaining)
        {
            var result = EvaluatePrimary(expr, out remaining);
            while (remaining.Length > 0 && remaining[0] == '&')
            {
                if (!result) return false;
                var rhs = EvaluatePrimary(remaining.Substring(1), out remaining);
                result = result && rhs;
            }
            return result;
        }

        // Primary: a parenthesised sub-expression or a bare module name
        private static bool EvaluatePrimary(string expr, out string remaining)
        {
            expr = expr.TrimStart();

            if (expr.Length > 0 && expr[0] == '(')
            {
                // Find matching closing paren, accounting for nesting
                var depth = 1;
                var i = 1;
                while (i < expr.Length && depth > 0)
                {
                    if (expr[i] == '(') depth++;
                    else if (expr[i] == ')') depth--;
                    i++;
                }
                // inner content is between the first '(' and the matching ')'
                var inner = expr.Substring(1, i - 2);
                remaining = expr.Substring(i).TrimStart();
                return EvaluateOr(inner, out _);
            }

            // Bare module name: read up to next operator or paren
            var match = tokenRegex.Match(expr);
            if (!match.Success || match.Groups[1].Length == 0)
            {
                remaining = string.Empty;
                return false;
            }

            var modName = match.Groups[1].Value.Trim();
            var op = match.Groups[2].Value;      // the delimiter that ended the name
            var rest = match.Groups[3].Value;    // everything after the delimiter

            // If the delimiter is a paren or operator, put it back so the caller can see it
            remaining = op == "|" || op == "&" || op == ")" || op == "("

                ? op + rest
                : rest; // op is "" (end of string)

            return IsModSingleEnabled(modName);
        }

        [WEFormula(typeof(int))]
        public static int IsModuleEnabled(Entity _, Dictionary<string, string> vars)
            => vars.TryGetValue("!module", out var module) && IsModuleEnabled(module) ? 1 : 0;
    }
}