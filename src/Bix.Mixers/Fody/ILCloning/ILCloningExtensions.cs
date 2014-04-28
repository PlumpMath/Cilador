﻿/***************************************************************************/
// Copyright 2013-2014 Riley White
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
/***************************************************************************/

using Mono.Cecil;
using Mono.Collections.Generic;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bix.Mixers.Fody.ILCloning
{
    internal static class ILCloningExtensions
    {
        public static void CloneStructure(this IEnumerable<IMemberCloner> cloners)
        {
            Contract.Requires(cloners != null);
            Contract.Requires(!cloners.Any(cloner => cloner.IsStructureCloned));
            Contract.Ensures(cloners.All(cloner => cloner.IsStructureCloned));

            foreach (var cloner in cloners) { cloner.CloneStructure(); }
        }

        [Pure]
        public static bool IsNestedWithin(this TypeReference type, TypeDefinition possibleAncestorType)
        {
            if (type == null || type.DeclaringType == null) { return false; }
            else if (type.DeclaringType.Resolve().FullName == possibleAncestorType.FullName) { return true; }
            else { return type.DeclaringType.IsNestedWithin(possibleAncestorType); }
        }

        [Pure]
        public static bool IsAnyTypeAncestorAGenericInstanceWithArgumentsIn(this MemberReference member, TypeDefinition argumentsSearchType)
        {
            if (member == null || member.DeclaringType == null)
            {
                return false;
            }
            else if (member.DeclaringType.IsGenericInstance &&
                     ((GenericInstanceType)member.DeclaringType).GenericArguments.Any(
                        genericArgument => genericArgument.IsNestedWithin(argumentsSearchType)))
            {
                return true;
            }
            else
            {
                return
                    member.DeclaringType != null &&
                    member.DeclaringType.IsAnyTypeAncestorAGenericInstanceWithArgumentsIn(argumentsSearchType);
            }
        }

        [Pure]
        public static bool SignatureEquals(this MethodReference target, MethodReference source, RootContext rootContext)
        {
            Contract.Requires(rootContext != null);

            if (target == null || source == null) { return target == null && source == null; }

            return target.FullName.Replace(rootContext.RootTarget.FullName, rootContext.RootSource.FullName) == source.FullName;
        }

        [Pure]
        public static bool SignatureEquals(this PropertyDefinition left, PropertyDefinition right)
        {
            if (left == null || right == null) { return left == null && right == null; }

            return left.FullName.Replace(left.DeclaringType.FullName + "::", string.Empty) == right.FullName.Replace(right.DeclaringType.FullName + "::", string.Empty);
        }

        public static void CloneAllParameters(
            this Collection<ParameterDefinition> targetParameters,
            Collection<ParameterDefinition> sourceParameters,
            RootContext rootContext,
            Dictionary<ParameterDefinition, ParameterDefinition> parameterOperandReplacementMap = null)
        {
            Contract.Requires(targetParameters != null);
            Contract.Requires(sourceParameters != null);
            Contract.Requires(targetParameters != sourceParameters);
            Contract.Requires(targetParameters.Count == 0);
            Contract.Requires(rootContext != null);
            Contract.Ensures(targetParameters.Count == sourceParameters.Count);

            foreach (var sourceParameter in sourceParameters)
            {
                var targetParameter =
                    new ParameterDefinition(sourceParameter.Name, sourceParameter.Attributes, rootContext.RootImport(sourceParameter.ParameterType));
                targetParameter.Constant = sourceParameter.Constant;
                targetParameter.HasConstant = sourceParameter.HasConstant;
                targetParameter.HasDefault = sourceParameter.HasDefault;
                targetParameter.HasFieldMarshal = sourceParameter.HasFieldMarshal;
                targetParameter.IsIn = sourceParameter.IsIn;
                targetParameter.IsLcid = sourceParameter.IsLcid;
                targetParameter.IsOptional = sourceParameter.IsOptional;
                targetParameter.IsOut = sourceParameter.IsOut;
                targetParameter.IsReturnValue = sourceParameter.IsReturnValue;

                // TODO research correct usage
                if (sourceParameter.MarshalInfo != null)
                {
                    targetParameter.MarshalInfo = new MarshalInfo(sourceParameter.MarshalInfo.NativeType);
                }

                // TODO research correct usage
                targetParameter.MetadataToken = new MetadataToken(sourceParameter.MetadataToken.TokenType, sourceParameter.MetadataToken.RID);

                // I did not check whether I get a similar issue here as with the duplication in the FieldCloner...adding a clear line just to make sure, though
                targetParameter.CustomAttributes.Clear();
                targetParameter.CloneAllCustomAttributes(sourceParameter, rootContext);

                targetParameters.Add(targetParameter);
                if (parameterOperandReplacementMap != null) { parameterOperandReplacementMap.Add(sourceParameter, targetParameter); }
            }
        }

        public static void CloneAllCustomAttributes(
            this ICustomAttributeProvider target,
            ICustomAttributeProvider source,
            RootContext rootContext)
        {
            Contract.Requires(target != null);
            Contract.Requires(target.CustomAttributes != null);
            Contract.Requires(target.CustomAttributes.Count == 0 || target == rootContext.RootTarget);
            Contract.Requires(source != null);
            Contract.Requires(source.CustomAttributes != null);
            Contract.Requires(target != source);
            Contract.Requires(rootContext != null);
            Contract.Ensures(
                target.CustomAttributes.Count == source.CustomAttributes.Count ||
                (target == rootContext.RootTarget && target.CustomAttributes.Count > source.CustomAttributes.Count));

            foreach (var sourceAttribute in source.CustomAttributes)
            {
                var targetAttribute = new CustomAttribute(rootContext.RootImport(sourceAttribute.Constructor));
                if (sourceAttribute.HasConstructorArguments)
                {
                    foreach (var sourceArgument in sourceAttribute.ConstructorArguments)
                    {
                        targetAttribute.ConstructorArguments.Add(
                            new CustomAttributeArgument(
                                rootContext.RootImport(sourceArgument.Type),
                                rootContext.DynamicRootImport(sourceArgument.Value)));
                    }
                }

                if (sourceAttribute.HasProperties)
                {
                    foreach (var sourceProperty in sourceAttribute.Properties)
                    {
                        targetAttribute.Properties.Add(
                            new CustomAttributeNamedArgument(
                                sourceProperty.Name,
                                new CustomAttributeArgument(
                                    rootContext.RootImport(sourceProperty.Argument.Type),
                                    rootContext.DynamicRootImport(sourceProperty.Argument.Value))));
                    }
                }
                target.CustomAttributes.Add(targetAttribute);
            }
        }
    }
}
