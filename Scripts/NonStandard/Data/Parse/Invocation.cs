using NonStandard.Extension;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace NonStandard.Data.Parse {
	public class Invocation {
		public object target;
		public object methodName;
		public Invocation(object target, object methodName) { this.target = target; this.methodName = methodName; }

		public static bool TryExecuteFunction(object scope, object resolvedFunctionIdentifier, Token arguments, out object funcResult, ITokenErrLog errLog, ResolvedEnoughDelegate isItResolvedEnough = null) {
			string funcName = GetMethodCall(resolvedFunctionIdentifier, arguments);
			if (funcName != null && TryExecuteFunction(scope, funcName, arguments, out funcResult, errLog, isItResolvedEnough)) {
				return true;
			}
			funcResult = null;
			return false;
		}
		private static string GetMethodCall(object result, Token args) { //int i, List<Token> tokens, List<int> found) {
			if (result is string funcName) {
				SyntaxTree e = args.GetAsSyntaxNode();
				if (e != null && e.IsEnclosure) {
					return funcName;
				}
			}
			return null;
		}

		private static bool TryExecuteFunction(object scope, string funcName, Token argsToken, out object result, ITokenErrLog tok, ResolvedEnoughDelegate isItResolvedEnough) {
			result = null;
			if (!DeterminePossibleMethods(scope, funcName, out List<MethodInfo> possibleMethods, tok, argsToken)) { return false; }
			List<object> args = ResolveFunctionArgumentList(argsToken, scope, tok, isItResolvedEnough);
			if (!DetermineValidMethods(funcName, argsToken, possibleMethods, out List<ParameterInfo[]> validParams, args, tok)) { return false; }
			if (!DetermineMethod(args, possibleMethods, validParams, out MethodInfo mi, out object[] finalArgs, tok, argsToken)) { return false; }
			return ExecuteMethod(scope, mi, finalArgs, out result, tok, argsToken);
		}
		private static bool DeterminePossibleMethods(object scope, string funcName, out List<MethodInfo> possibleMethods, ITokenErrLog tok, Token argsToken) {
			if (scope == null) {
				tok.AddError(argsToken, $"can't execute function \'{funcName}\' without scope");
				possibleMethods = null;
				return false;
			}
			possibleMethods = scope.GetType().GetMethods().FindAll(m => m.Name == funcName);
			if (possibleMethods.Count == 0) {
				tok.AddError(argsToken, $"missing function \'{funcName}\' in {scope.GetType()}");
				return false;
			}
			return true;
		}
		private static List<object> ResolveFunctionArgumentList(Token argsToken, object scope, ITokenErrLog tok, ResolvedEnoughDelegate isItResolvedEnough) {
			object argsRaw = argsToken.Resolve(tok, scope, isItResolvedEnough);
			if (argsRaw == null) { argsRaw = new List<object>(); }
			List<object> args = argsRaw as List<object>;
			if (args == null) {
				args = new List<object> { argsRaw };
			}
			// remove commas if they are comma tokens before and after being parsed
			SyntaxTree beforeParse = argsToken.GetAsSyntaxNode();
			for (int i = args.Count - 1; i >= 0; --i) {
				if ((args[i] as string) == "," && beforeParse.tokens[i + 1].StringifySmall() == ",") { args.RemoveAt(i); }
			}
			return args;
		}
		private static bool DetermineValidMethods(string funcName, Token argsToken, List<MethodInfo> possibleMethods, out List<ParameterInfo[]> validParams, IList<object> args, ITokenErrLog tok) {
			ParameterInfo[] pi;
			validParams = new List<ParameterInfo[]>();
			List<ParameterInfo[]> invalidParams = new List<ParameterInfo[]>();
			for (int i = possibleMethods.Count - 1; i >= 0; --i) {
				pi = possibleMethods[i].GetParameters();
				if (pi.Length != args.Count) {
					possibleMethods.RemoveAt(i);
					invalidParams.Add(pi);
					continue;
				}
				validParams.Add(pi);
			}
			// check arguments. start with the argument count
			if (possibleMethods.Count == 0) {
				tok.AddError(argsToken, $"'{funcName}' needs {invalidParams.JoinToString(" or ", par => par.Length.ToString())} arguments, not {args.Count} from {args.StringifySmall()}");
				return false;
			}
			return true;
		}
		private static bool DetermineMethod(List<object> args, List<MethodInfo> possibleMethods, List<ParameterInfo[]> validParams, out MethodInfo mi, out object[] finalArgs, ITokenErrLog tok, Token argsToken) {
			mi = null;
			finalArgs = new object[args.Count];
			for (int paramSet = 0; paramSet < validParams.Count; ++paramSet) {
				bool typesOk = true;
				ParameterInfo[] pi = validParams[paramSet];
				int a;
				if ((a = TryConvertArgs(args, finalArgs, pi, tok, argsToken)) != args.Count
				// it's only a problem if there are no other options
				&& paramSet == validParams.Count - 1) {
					tok.AddError(argsToken, $"can't convert \'{args[a]}\' to {pi[a].ParameterType} for {possibleMethods[paramSet].Name}{argsToken.Stringify()}");
				}
				if (typesOk) {
					mi = possibleMethods[paramSet];
					return true;
				}
			}
			return false;
		}
		private static int TryConvertArgs(IList<object> args, IList<object> finalArgs, ParameterInfo[] pi, ITokenErrLog tok, Token argsToken) {
			for (int i = 0; i < args.Count; ++i) {
				try {
					finalArgs[i] = Convert.ChangeType(args[i], pi[i].ParameterType);
				} catch (Exception) {
					return i;
				}
			}
			return args.Count;
		}
		private static bool ExecuteMethod(object scope, MethodInfo mi, object[] finalArgs, out object result, ITokenErrLog tok, Token argsToken) {
			try {
				result = mi.Invoke(scope, finalArgs);
			} catch (Exception e) {
				result = null;
				tok.AddError(argsToken, e.ToString());
				return false;
			}
			return true;
		}
	}

}