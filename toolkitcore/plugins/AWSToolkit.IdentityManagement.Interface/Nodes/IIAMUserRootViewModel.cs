﻿using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.IdentityManagement.Nodes
{
    public interface IIAMUserRootViewModel : IViewModel
    {
        IAmazonIdentityManagementService IAMClient { get; }

        void AddUser(User user);
        void RemoveUser(string username);
    }
}
