using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NonStandard.Data.Parse {
	public class ParserStackOfTokens {
		public List<TokenLayerBeingParsed> _stackOfTokensBeingParsed = new List<TokenLayerBeingParsed>();
		public bool IsParsing => _stackOfTokensBeingParsed.Count > 0 && Current._currentTokenIndex < Current.Tokens.Count;
		public int[] GetCurrentTokenIndex() {
			int[] index = new int[_stackOfTokensBeingParsed.Count];
			for (int i = 0; i < _stackOfTokensBeingParsed.Count; ++i) {
				index[i] = _stackOfTokensBeingParsed[i]._currentTokenIndex;
			}
			return index;
		}

		internal TokenLayerBeingParsed Current => _stackOfTokensBeingParsed[_stackOfTokensBeingParsed.Count - 1];
		public void PushTokensBeingParsed(List<Token> tokenList, int index = 0) {
			_stackOfTokensBeingParsed.Add(new TokenLayerBeingParsed(tokenList, index));
		}
		public bool Increment() {
			if (_stackOfTokensBeingParsed.Count <= 0) return false;
			TokenLayerBeingParsed pstate = PeekTokenBeingParsed();
			//Show.Warning(pstate.GetToken());
			++pstate._currentTokenIndex;
			while (pstate._currentTokenIndex >= pstate.Tokens.Count) {
				PopTokensBeingParsed();
				if (_stackOfTokensBeingParsed.Count <= 0) return false;
				pstate = PeekTokenBeingParsed();
				++pstate._currentTokenIndex;
			}
			return true;
		}
		protected void PopTokensBeingParsed() {
			_stackOfTokensBeingParsed.RemoveAt(_stackOfTokensBeingParsed.Count - 1);
		}
		protected TokenLayerBeingParsed PeekTokenBeingParsed() {
			return _stackOfTokensBeingParsed[_stackOfTokensBeingParsed.Count - 1];
		}

		internal void StartStack(List<Token> tokens) {
			_stackOfTokensBeingParsed.Clear();
			PushTokensBeingParsed(tokens);
		}
	}
	public class TokenLayerBeingParsed {
		public int _currentTokenIndex = 0; // TODO make this protected
		private List<Token> _tokens;
		public List<Token> Tokens => _tokens;
		public Token Token { get { return _tokens[_currentTokenIndex]; } }
		public bool IsValid => _currentTokenIndex >= 0 && _currentTokenIndex < _tokens.Count;
		public TokenLayerBeingParsed(List<Token> tokens, int index) { _tokens = tokens; _currentTokenIndex = 0; }
	}


}
