﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Roslyn.Compilers;
using Roslyn.Compilers.Common;
using Roslyn.Services;
using Roslyn.Compilers.CSharp;

namespace FragSharp
{
    static class SymbolInfoExtension
    {
        public static Symbol GetSymbolOrSetError(this SymbolInfo info, ref string LastError)
        {
            if (info.Symbol != null)
            {
                return info.Symbol;
            }
            else
            {
                LastError = info.CandidateReason.ToString();
                return null;
            }
        }
    }

    static class SymbolExtension
    {
        public static bool DerivesFrom(this TypeSymbol symbol, string name)
        {
            if (symbol.BaseType == null) return false;
            else return
                    symbol.BaseType.Name == name ||
                    symbol.BaseType.DerivesFrom(name);
        }

        public static bool DerivesFrom(this NamedTypeSymbol symbol, string name)
        {
            if (symbol.BaseType == null) return false;
            else return
                    symbol.BaseType.Name == name ||
                    symbol.BaseType.DerivesFrom(name);
        }

        public static AttributeData GetAttribute(this Symbol symbol, string AttributeName)
        {
            string FullAttributeName = AttributeName + "Attribute";

            var attributes = symbol.GetAttributes();
            if (attributes.Count == 0) return null;

            var attribute = attributes.FirstOrDefault(data => data.AttributeClass.Name == FullAttributeName);

            return attribute;
        }

        public static bool HasAttribute(this Symbol symbol, string AttributeName)
        {
            return symbol.GetAttribute(AttributeName) != null;
        }
    }
}
