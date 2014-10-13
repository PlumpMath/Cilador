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
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bix.Mixers.Fody.ILCloning
{
    /// <summary>
    /// Simple type to hold and invoke collections of cloners for IL cloning.
    /// </summary>
    internal class Cloners
    {
        /// <summary>
        /// Creates a new <see cref="Cloners"/>
        /// </summary>
        public Cloners()
        {
            Contract.Ensures(this.TypeCloners != null);
            Contract.Ensures(this.FieldCloners != null);
            Contract.Ensures(this.MethodSignatureCloners != null);
            Contract.Ensures(this.MethodParameterCloners != null);
            Contract.Ensures(this.MethodBodyCloners != null);
            Contract.Ensures(this.VariableCloners != null);
            Contract.Ensures(this.InstructionCloners != null);
            Contract.Ensures(this.ExceptionHandlerCloners != null);
            Contract.Ensures(this.PropertyCloners != null);
            Contract.Ensures(this.EventCloners != null);
            Contract.Ensures(this.GenericParameterCloners != null);
            Contract.Ensures(this.TargetTypeBySourceFullName != null);
            Contract.Ensures(this.TargetFieldBySourceFullName != null);
            Contract.Ensures(this.TargetMethodBySourceFullName != null);
            Contract.Ensures(this.TargetPropertyBySourceFullName != null);
            Contract.Ensures(this.TargetEventBySourceFullName != null);

            this.TypeCloners = new List<TypeCloner>();
            this.FieldCloners = new List<FieldCloner>();
            this.MethodSignatureCloners = new List<MethodSignatureCloner>();
            this.MethodParameterCloners = new List<ParameterCloner>();
            this.MethodBodyCloners = new List<MethodBodyCloner>();
            this.VariableCloners = new List<VariableCloner>();
            this.InstructionCloners = new List<InstructionCloner>();
            this.ExceptionHandlerCloners = new List<ExceptionHandlerCloner>();
            this.PropertyCloners = new List<PropertyCloner>();
            this.EventCloners = new List<EventCloner>();
            this.GenericParameterCloners = new List<GenericParameterCloner>();

            this.TargetTypeBySourceFullName = new Dictionary<string, TypeDefinition>();
            this.TargetGenericParameterGetterBySourceOwnerFullNameAndPosition = new Dictionary<string, Func<GenericParameter>>();
            this.TargetFieldBySourceFullName = new Dictionary<string, FieldDefinition>();
            this.TargetMethodBySourceFullName = new Dictionary<string, MethodDefinition>();
            this.TargetPropertyBySourceFullName = new Dictionary<string, PropertyDefinition>();
            this.TargetEventBySourceFullName = new Dictionary<string, EventDefinition>();
        }

        /// <summary>
        /// Gets or sets whether the cloners are currenty being invoked.
        /// </summary>
        public bool AreAllClonersAdded { get; private set; }

        /// <summary>
        /// Marks that all cloners have been added. Targets may be looked
        /// up and cloning may be invoked only after this has been called.
        /// </summary>
        public void SetAllClonersAdded()
        {
            this.AreAllClonersAdded = true;
        }

        /// <summary>
        /// Gets or sets whether the cloners are currently being invoked.
        /// </summary>
        public bool AreClonersInvoking { get; private set; }

        /// <summary>
        /// Gets or sets whether the cloners are have been invoked.
        /// </summary>
        public bool AreClonersInvoked { get; private set; }

        /// <summary>
        /// Gets whether the cloners may be invoked.
        /// </summary>
        public bool CanInvokeCloners
        {
            get { return this.AreAllClonersAdded && !this.AreClonersInvoking && !this.AreClonersInvoked; }
        }

        /// <summary>
        /// Invokes all contained cloners.
        /// </summary>
        public void InvokeCloners()
        {
            Contract.Requires(this.CanInvokeCloners);
            Contract.Ensures(!this.AreClonersInvoking);
            Contract.Ensures(this.AreClonersInvoked);
            Contract.Ensures(!this.CanInvokeCloners);

            this.AreClonersInvoking = true;

            this.GenericParameterCloners.Clone();
            this.TypeCloners.Clone();
            this.FieldCloners.Clone();
            this.MethodSignatureCloners.Clone();
            this.MethodParameterCloners.Clone();
            this.PropertyCloners.Clone();
            this.EventCloners.Clone();
            this.VariableCloners.Clone();
            this.MethodBodyCloners.Clone();
            this.InstructionCloners.Clone();
            this.ExceptionHandlerCloners.Clone();

            this.AreClonersInvoked = true;
            this.AreClonersInvoking = false;
        }

        #region Types

        /// <summary>
        /// Gets or sets the collection of target types indexed by full name of the cloning source.
        /// </summary>
        private Dictionary<string, TypeDefinition> TargetTypeBySourceFullName { get; set; }

        /// <summary>
        /// Gets or sets cloners for all types to be cloned.
        /// </summary>
        private List<TypeCloner> TypeCloners { get; set; }

        /// <summary>
        /// Adds a cloner to the collection.
        /// </summary>
        /// <param name="cloner">Cloner to add to the collection.</param>
        public void AddCloner(TypeCloner cloner)
        {
            Contract.Requires(cloner != null);
            Contract.Requires(!this.AreAllClonersAdded);

            this.TypeCloners.Add(cloner);
            this.TargetTypeBySourceFullName.Add(GetUniqueKeyFor(cloner.Source), cloner.Target);
        }

        /// <summary>
        /// Attempt to retrieve the cloning target for a given source.
        /// </summary>
        /// <param name="source">Source to find target for.</param>
        /// <param name="target">Target for the given source if it exists, else <c>null</c>.</param>
        /// <returns><c>true</c> if a target was found, else <c>false</c>.</returns>
        public bool TryGetTargetFor(TypeReference source, out TypeDefinition target)
        {
            Contract.Requires(source != null);
            Contract.Requires(!source.IsArray);
            Contract.Requires(!source.IsGenericInstance);
            Contract.Requires(!source.IsGenericParameter);
            Contract.Requires(this.AreAllClonersAdded);
            Contract.Ensures(Contract.ValueAtReturn(out target) != null || !Contract.Result<bool>());

            return this.TargetTypeBySourceFullName.TryGetValue(GetUniqueKeyFor(source.Resolve()), out target);
        }

        #endregion

        #region Generic Parameters

        /// <summary>
        /// Gets or sets the collection of target generic parameters indexed by full owner name and position of the cloning source.
        /// </summary>
        private Dictionary<string, Func<GenericParameter>> TargetGenericParameterGetterBySourceOwnerFullNameAndPosition { get; set; }

        /// <summary>
        /// Gets or sets cloners for all generic parameters to be cloned.
        /// </summary>
        private List<GenericParameterCloner> GenericParameterCloners { get; set; }

        /// <summary>
        /// Adds a cloner to the collection.
        /// </summary>
        /// <param name="cloner">Cloner to add to the collection.</param>
        public void AddCloner(GenericParameterCloner cloner)
        {
            Contract.Requires(cloner != null);
            Contract.Requires(cloner.Source.IsGetAccessor && cloner.Target.IsGetAccessor);
            Contract.Requires(!this.AreAllClonersAdded);

            this.GenericParameterCloners.Add(cloner);
            this.TargetGenericParameterGetterBySourceOwnerFullNameAndPosition.Add(GetUniqueKeyFor(cloner.Source.Getter()), cloner.Target.Getter);
        }

        /// <summary>
        /// Attempt to retrieve the cloning target for a given source.
        /// </summary>
        /// <param name="source">Source to find target for.</param>
        /// <param name="target">Target for the given source if it exists, else <c>null</c>.</param>
        /// <returns><c>true</c> if a target was found, else <c>false</c>.</returns>
        public bool TryGetTargetFor(GenericParameter source, out GenericParameter target)
        {
            Contract.Requires(source != null);
            Contract.Requires(this.AreAllClonersAdded);
            Contract.Ensures(Contract.ValueAtReturn(out target) != null || !Contract.Result<bool>());

            Func<GenericParameter> targetGetter;
            if (!this.TargetGenericParameterGetterBySourceOwnerFullNameAndPosition.TryGetValue(GetUniqueKeyFor(source), out targetGetter))
            {
                target = null;
                return false;
            }

            Contract.Assert(targetGetter != null);

            target = targetGetter();

            // most cloned items have target object created at cloner gathering time,
            // but because target generic parameters are not created until clone time,
            // we need to be more careful here

            // this check has a dependency on the implementation deatil of using a dummy
            // generic parameter with the owner set to the void type
            if (target.Owner is TypeReference && ((MemberReference)target.Owner).FullName == typeof(void).FullName)
            {
                throw new InvalidOperationException(string.Format(
                    "Tried to find target for source [{0}] with owner [{1}], but it has not yet be cloned.",
                    source.Name,
                    ((MemberReference)source.Owner).FullName));
            }

            return target != null;
        }

        #endregion

        #region Fields

        /// <summary>
        /// Gets or sets the collection of target field indexed by full name of the cloning source.
        /// </summary>
        private Dictionary<string, FieldDefinition> TargetFieldBySourceFullName { get; set; }

        /// <summary>
        /// Gets or sets cloners for all fields to be cloned.
        /// </summary>
        private List<FieldCloner> FieldCloners { get; set; }

        /// <summary>
        /// Adds a cloner to the collection.
        /// </summary>
        /// <param name="cloner">Cloner to add to the collection.</param>
        public void AddCloner(FieldCloner cloner)
        {
            Contract.Requires(cloner != null);
            Contract.Requires(!this.AreAllClonersAdded);

            this.FieldCloners.Add(cloner);
            this.TargetFieldBySourceFullName.Add(GetUniqueKeyFor(cloner.Source), cloner.Target);
        }

        /// <summary>
        /// Attempt to retrieve the cloning target for a given source.
        /// </summary>
        /// <param name="source">Source to find target for.</param>
        /// <param name="target">Target for the given source if it exists, else <c>null</c>.</param>
        /// <returns><c>true</c> if a target was found, else <c>false</c>.</returns>
        public bool TryGetTargetFor(FieldReference source, out FieldDefinition target)
        {
            Contract.Requires(source != null);
            Contract.Requires(this.AreAllClonersAdded);
            Contract.Ensures(Contract.ValueAtReturn(out target) != null || !Contract.Result<bool>());

            return this.TargetFieldBySourceFullName.TryGetValue(GetUniqueKeyFor(source.Resolve()), out target);
        }

        #endregion

        #region Method Signatures

        /// <summary>
        /// Gets or sets the collection of target method indexed by full name of the cloning source.
        /// </summary>
        private Dictionary<string, MethodDefinition> TargetMethodBySourceFullName { get; set; }

        /// <summary>
        /// Gets or sets cloners for all method signatures to be cloned.
        /// </summary>
        private List<MethodSignatureCloner> MethodSignatureCloners { get; set; }

        /// <summary>
        /// Adds a cloner to the collection.
        /// </summary>
        /// <param name="cloner">Cloner to add to the collection.</param>
        public void AddCloner(MethodSignatureCloner cloner)
        {
            Contract.Requires(cloner != null);
            Contract.Requires(!this.AreAllClonersAdded);

            this.MethodSignatureCloners.Add(cloner);
            this.TargetMethodBySourceFullName.Add(GetUniqueKeyFor(cloner.Source), cloner.Target);
        }

        /// <summary>
        /// Attempt to retrieve the cloning target for a given source.
        /// </summary>
        /// <param name="source">Source to find target for.</param>
        /// <param name="target">Target for the given source if it exists, else <c>null</c>.</param>
        /// <returns><c>true</c> if a target was found, else <c>false</c>.</returns>
        public bool TryGetTargetFor(MethodReference source, out MethodDefinition target)
        {
            Contract.Requires(source != null);
            Contract.Requires(!source.IsGenericInstance);
            Contract.Requires(this.AreAllClonersAdded);
            Contract.Ensures(Contract.ValueAtReturn(out target) != null || !Contract.Result<bool>());

            return this.TargetMethodBySourceFullName.TryGetValue(GetUniqueKeyFor(source.Resolve()), out target);
        }

        #endregion

        #region Method Parameters

        /// <summary>
        /// Gets or sets cloners for all method parameters to be cloned.
        /// </summary>
        private List<ParameterCloner> MethodParameterCloners { get; set; }

        /// <summary>
        /// Adds a cloner to the collection.
        /// </summary>
        /// <param name="cloners">Cloners to add to the collection.</param>
        public void AddCloners(IEnumerable<ParameterCloner> cloners)
        {
            Contract.Requires(cloners != null);
            Contract.Requires(!cloners.Any(cloner => cloner == null));
            Contract.Requires(!this.AreAllClonersAdded);

            this.MethodParameterCloners.AddRange(cloners); ;
        }

        #endregion

        #region Method Bodies

        /// <summary>
        /// Gets or sets cloners for all method bodies to be cloned.
        /// </summary>
        private List<MethodBodyCloner> MethodBodyCloners { get; set; }

        /// <summary>
        /// Adds a cloner to the collection.
        /// </summary>
        /// <param name="cloner">Cloner to add to the collection.</param>
        public void AddCloner(MethodBodyCloner cloner)
        {
            Contract.Requires(cloner != null);
            Contract.Requires(!this.AreAllClonersAdded);

            this.MethodBodyCloners.Add(cloner);
        }

        #endregion

        #region Method Body Variables

        /// <summary>
        /// Gets or sets cloners for all variables to be cloned.
        /// </summary>
        private List<VariableCloner> VariableCloners { get; set; }

        /// <summary>
        /// Adds a cloner to the collection.
        /// </summary>
        /// <param name="cloners">Cloners to add to the collection.</param>
        public void AddCloners(IEnumerable<VariableCloner> cloners)
        {
            Contract.Requires(cloners != null);
            Contract.Requires(!cloners.Any(cloner => cloner == null));
            Contract.Requires(!this.AreAllClonersAdded);

            this.VariableCloners.AddRange(cloners);
        }

        #endregion

        #region Method Body Instructions

        /// <summary>
        /// Gets or sets cloners for all IL instructions to be cloned.
        /// </summary>
        private List<InstructionCloner> InstructionCloners { get; set; }

        /// <summary>
        /// Adds a cloner to the collection.
        /// </summary>
        /// <param name="cloners">Cloners to add to the collection.</param>
        public void AddCloners(IEnumerable<InstructionCloner> cloners)
        {
            Contract.Requires(cloners != null);
            Contract.Requires(!cloners.Any(cloner => cloner == null));
            Contract.Requires(!this.AreAllClonersAdded);

            this.InstructionCloners.AddRange(cloners);
        }

        #endregion

        #region Method Body Exception Handlers

        /// <summary>
        /// Gets or sets cloners for all exception handlers to be cloned.
        /// </summary>
        private List<ExceptionHandlerCloner> ExceptionHandlerCloners { get; set; }

        /// <summary>
        /// Adds a cloner to the collection.
        /// </summary>
        /// <param name="cloners">Cloners to add to the collection.</param>
        public void AddCloners(IEnumerable<ExceptionHandlerCloner> cloners)
        {
            Contract.Requires(cloners != null);
            Contract.Requires(!cloners.Any(cloner => cloner == null));
            Contract.Requires(!this.AreAllClonersAdded);

            this.ExceptionHandlerCloners.AddRange(cloners);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the collection of target property indexed by full name of the cloning source.
        /// </summary>
        private Dictionary<string, PropertyDefinition> TargetPropertyBySourceFullName { get; set; }

        /// <summary>
        /// Gets or sets cloners for all properties to be cloned.
        /// </summary>
        private List<PropertyCloner> PropertyCloners { get; set; }

        /// <summary>
        /// Adds a cloner to the collection.
        /// </summary>
        /// <param name="cloner">Cloner to add to the collection.</param>
        public void AddCloner(PropertyCloner cloner)
        {
            Contract.Requires(cloner != null);
            Contract.Requires(!this.AreAllClonersAdded);

            this.PropertyCloners.Add(cloner);
            this.TargetPropertyBySourceFullName.Add(GetUniqueKeyFor(cloner.Source), cloner.Target);
        }

        #endregion

        #region Events

        /// <summary>
        /// Gets or sets the collection of target event indexed by full name of the cloning source.
        /// </summary>
        private Dictionary<string, EventDefinition> TargetEventBySourceFullName { get; set; }

        /// <summary>
        /// Gets or sets cloners for all events to be cloned.
        /// </summary>
        private List<EventCloner> EventCloners { get; set; }

        /// <summary>
        /// Adds a cloner to the collection.
        /// </summary>
        /// <param name="cloner">Cloner to add to the collection.</param>
        public void AddCloner(EventCloner cloner)
        {
            Contract.Requires(cloner != null);
            Contract.Requires(!this.AreAllClonersAdded);

            this.EventCloners.Add(cloner);
            this.TargetEventBySourceFullName.Add(GetUniqueKeyFor(cloner.Source), cloner.Target);
        }

        #endregion

        /// <summary>
        /// Gets a unique string key for an item that is suitable for use in dictionaries and hashes.
        /// </summary>
        /// <param name="item">Item that will need indexed.</param>
        /// <returns>Unique index value for the item.</returns>
        private static string GetUniqueKeyFor(IMemberDefinition item)
        {
            return item.FullName;
        }

        /// <summary>
        /// Gets a unique string key for an item that is suitable for use in dictionaries and hashes.
        /// </summary>
        /// <param name="item">Item that will need indexed.</param>
        /// <returns>Unique index value for the item.</returns>
        public static string GetUniqueKeyFor(GenericParameter item)
        {
            switch(item.Owner.GenericParameterType)
            {
                case GenericParameterType.Method:
                    return string.Format(
                        "{0}:::{1}:::{2}",
                        item.DeclaringMethod.DeclaringType.FullName,
                        item.DeclaringMethod.Name,
                        item.Position);

                case GenericParameterType.Type:
                    return string.Format(
                        "{0}:::{1}",
                        item.DeclaringType.FullName,
                        item.Position);

                default:
                    throw new InvalidOperationException(string.Format(
                        "Unknwon Generic parameter type [{0}] for a generic parameter's owner.",
                        item.Owner.GenericParameterType.ToString()));
            }
        }
    }
}
