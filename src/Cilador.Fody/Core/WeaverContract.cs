﻿/***************************************************************************/
// Copyright 2013-2015 Riley White
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

using System;
using System.Diagnostics.Contracts;
using Mono.Cecil;

namespace Cilador.Fody.Core
{
    using Cilador.Fody.Config;

    /// <summary>
    /// Contracts for <see cref="IWeaver"/> implementations.
    /// </summary>
    [ContractClassFor(typeof(IWeaver))]
    internal abstract class WeaverContract : IWeaver
    {
        /// <summary>
        /// Contracts for <see cref="IWeaver.IsInitialized"/>
        /// </summary>
        public bool IsInitialized
        {
            get { throw new NotSupportedException(); }
        }

        /// <summary>
        /// Contracts for <see cref="IWeaver.Initialize"/>
        /// </summary>
        /// <param name="weavingContext">Context data for command initialization.</param>
        /// <param name="config">Configuration data for the command. Commands may require particular types for this argument that are subtypes of <see cref="WeaverConfigTypeBase"/></param>
        public void Initialize(IWeavingContext weavingContext, WeaverConfigTypeBase config)
        {
            Contract.Requires(weavingContext != null);
            Contract.Requires(config != null);
            Contract.Requires(!this.IsInitialized);
            Contract.Ensures(this.IsInitialized);

            throw new NotSupportedException();
        }

        /// <summary>
        /// Contracts for <see cref="IWeaver.Weave"/>
        /// </summary>
        /// <param name="weavingContext">Context data for weaving.</param>
        /// <param name="target">The type to which the weave will be applied/</param>
        /// <param name="weaveAttribute">Attribute that may contain arguments for the weaver invocation.</param>
        public void Weave(IWeavingContext weavingContext, TypeDefinition target, CustomAttribute weaveAttribute)
        {
            Contract.Requires(weavingContext != null);
            Contract.Requires(target != null);
            Contract.Requires(weaveAttribute != null);
            Contract.Requires(this.IsInitialized);

            throw new NotSupportedException();
        }
    }
}
