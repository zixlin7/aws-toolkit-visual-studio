/*
 * Copyright 2010-2014 Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License").
 * You may not use this file except in compliance with the License.
 * A copy of the License is located at
 * 
 *  http://aws.amazon.com/apache2.0
 * 
 * or in the "license" file accompanying this file. This file is distributed
 * on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either
 * express or implied. See the License for the specific language governing
 * permissions and limitations under the License.
 */

/*
 * Do not modify this file. This file is generated from the telemetry-2017-07-25.normal.json service model.
 */

using System;

using Amazon.Runtime;

namespace Amazon.ToolkitTelemetry
{

    /// <summary>
    /// Constants used for properties of type AWSProduct.
    /// </summary>
    public class AWSProduct : ConstantClass
    {

        /// <summary>
        /// Constant AWSToolkitForEclipse for AWSProduct
        /// </summary>
        public static readonly AWSProduct AWSToolkitForEclipse = new AWSProduct("AWS Toolkit For Eclipse");
        /// <summary>
        /// Constant AWSToolkitForJetBrains for AWSProduct
        /// </summary>
        public static readonly AWSProduct AWSToolkitForJetBrains = new AWSProduct("AWS Toolkit For JetBrains");
        /// <summary>
        /// Constant AWSToolkitForVisualStudio for AWSProduct
        /// </summary>
        public static readonly AWSProduct AWSToolkitForVisualStudio = new AWSProduct("AWS Toolkit For VisualStudio");
        /// <summary>
        /// Constant AWSToolkitForVSCode for AWSProduct
        /// </summary>
        public static readonly AWSProduct AWSToolkitForVSCode = new AWSProduct("AWS Toolkit For VS Code");
        /// <summary>
        /// Constant Canary for AWSProduct
        /// </summary>
        public static readonly AWSProduct Canary = new AWSProduct("canary");

        /// <summary>
        /// This constant constructor does not need to be called if the constant
        /// you are attempting to use is already defined as a static instance of 
        /// this class.
        /// This constructor should be used to construct constants that are not
        /// defined as statics, for instance if attempting to use a feature that is
        /// newer than the current version of the SDK.
        /// </summary>
        public AWSProduct(string value)
            : base(value)
        {
        }

        /// <summary>
        /// Finds the constant for the unique value.
        /// </summary>
        /// <param name="value">The unique value for the constant</param>
        /// <returns>The constant for the unique value</returns>
        public static AWSProduct FindValue(string value)
        {
            return FindValue<AWSProduct>(value);
        }

        /// <summary>
        /// Utility method to convert strings to the constant class.
        /// </summary>
        /// <param name="value">The string value to convert to the constant class.</param>
        /// <returns></returns>
        public static implicit operator AWSProduct(string value)
        {
            return FindValue(value);
        }
    }


    /// <summary>
    /// Constants used for properties of type Sentiment.
    /// </summary>
    public class Sentiment : ConstantClass
    {

        /// <summary>
        /// Constant Negative for Sentiment
        /// </summary>
        public static readonly Sentiment Negative = new Sentiment("Negative");
        /// <summary>
        /// Constant Positive for Sentiment
        /// </summary>
        public static readonly Sentiment Positive = new Sentiment("Positive");

        /// <summary>
        /// This constant constructor does not need to be called if the constant
        /// you are attempting to use is already defined as a static instance of 
        /// this class.
        /// This constructor should be used to construct constants that are not
        /// defined as statics, for instance if attempting to use a feature that is
        /// newer than the current version of the SDK.
        /// </summary>
        public Sentiment(string value)
            : base(value)
        {
        }

        /// <summary>
        /// Finds the constant for the unique value.
        /// </summary>
        /// <param name="value">The unique value for the constant</param>
        /// <returns>The constant for the unique value</returns>
        public static Sentiment FindValue(string value)
        {
            return FindValue<Sentiment>(value);
        }

        /// <summary>
        /// Utility method to convert strings to the constant class.
        /// </summary>
        /// <param name="value">The string value to convert to the constant class.</param>
        /// <returns></returns>
        public static implicit operator Sentiment(string value)
        {
            return FindValue(value);
        }
    }


    /// <summary>
    /// Constants used for properties of type Unit.
    /// </summary>
    public class Unit : ConstantClass
    {

        /// <summary>
        /// Constant Bytes for Unit
        /// </summary>
        public static readonly Unit Bytes = new Unit("Bytes");
        /// <summary>
        /// Constant Count for Unit
        /// </summary>
        public static readonly Unit Count = new Unit("Count");
        /// <summary>
        /// Constant Milliseconds for Unit
        /// </summary>
        public static readonly Unit Milliseconds = new Unit("Milliseconds");
        /// <summary>
        /// Constant None for Unit
        /// </summary>
        public static readonly Unit None = new Unit("None");
        /// <summary>
        /// Constant Percent for Unit
        /// </summary>
        public static readonly Unit Percent = new Unit("Percent");

        /// <summary>
        /// This constant constructor does not need to be called if the constant
        /// you are attempting to use is already defined as a static instance of 
        /// this class.
        /// This constructor should be used to construct constants that are not
        /// defined as statics, for instance if attempting to use a feature that is
        /// newer than the current version of the SDK.
        /// </summary>
        public Unit(string value)
            : base(value)
        {
        }

        /// <summary>
        /// Finds the constant for the unique value.
        /// </summary>
        /// <param name="value">The unique value for the constant</param>
        /// <returns>The constant for the unique value</returns>
        public static Unit FindValue(string value)
        {
            return FindValue<Unit>(value);
        }

        /// <summary>
        /// Utility method to convert strings to the constant class.
        /// </summary>
        /// <param name="value">The string value to convert to the constant class.</param>
        /// <returns></returns>
        public static implicit operator Unit(string value)
        {
            return FindValue(value);
        }
    }

}