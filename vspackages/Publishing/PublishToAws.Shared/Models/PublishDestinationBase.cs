﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Media;

using Amazon.AWSToolkit.Publish.Models.Configuration;
using Amazon.AWSToolkit.Publish.Util;

namespace Amazon.AWSToolkit.Publish.Models
{
    /// <summary>
    /// Represents a destination and technique for deploying an application.
    /// </summary>
    [DebuggerDisplay("{RecipeId} | {Name}")]
    public abstract class PublishDestinationBase : IEquatable<PublishDestinationBase>
    {
        public string Name { get; protected set; }

        /// <summary>
        /// Whether or not this is the most recommended publish target.
        /// </summary>
        public bool IsRecommended { get; set; } = false;

        public string RecipeId { get; protected set; }
        public string BaseRecipeId { get; protected set; }
        public string RecipeName { get; protected set; }

        public string Description { get; protected set; }
        public string ShortDescription { get; protected set; }

        public List<Category> ConfigurationCategories { get; } = new List<Category>();

        /// <summary>
        /// Does the deployment target originate from a generated deployment project
        /// </summary>
        public bool IsGenerated { get; protected set; }

        public DeploymentArtifact DeploymentArtifact { get; protected set; }

        public string Service { get; protected set; }
        public ImageSource ServiceIcon => RecipeServiceImageResolver.GetServiceImage(Service);

        protected PublishDestinationBase()
        {
            ConfigurationCategories.Add(new Category()
            {
                Id = Category.FallbackCategoryId,
                DisplayName = "Other",
                Order = 0,
            });
        }

        public bool Equals(PublishDestinationBase other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Name == other.Name &&
                   IsRecommended == other.IsRecommended &&
                   RecipeId == other.RecipeId &&
                   BaseRecipeId == other.BaseRecipeId &&
                   RecipeName == other.RecipeName &&
                   Description == other.Description &&
                   IsGenerated == other.IsGenerated &&
                   ShortDescription == other.ShortDescription &&
                   ConfigurationCategories.SequenceEqual(other.ConfigurationCategories) &&
                   DeploymentArtifact == other.DeploymentArtifact &&
                   Service == other.Service;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((PublishDestinationBase) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ IsRecommended.GetHashCode();
                hashCode = (hashCode * 397) ^ (RecipeId != null ? RecipeId.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (BaseRecipeId != null ? BaseRecipeId.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (RecipeName != null ? RecipeName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Description != null ? Description.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ IsGenerated.GetHashCode();
                hashCode = (hashCode * 397) ^ (ShortDescription != null ? ShortDescription.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ ConfigurationCategories.GetHashCode();
                hashCode = (hashCode * 397) ^ DeploymentArtifact.GetHashCode();
                hashCode = (hashCode * 397) ^ (Service != null ? Service.GetHashCode() : 0);
                return hashCode;
            }
        }

        public static bool operator ==(PublishDestinationBase left, PublishDestinationBase right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(PublishDestinationBase left, PublishDestinationBase right)
        {
            return !Equals(left, right);
        }
    }
}
