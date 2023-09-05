using System;

using Amazon.AWSToolkit.Credentials.IO;
using Amazon.Runtime.CredentialManagement;

using Xunit;

namespace AWSToolkit.Tests.Credentials.IO
{
    public class MemoryCredentialsFileTests
    {
        private readonly MemoryCredentialsFile _sut;

        private readonly CredentialProfile _fromProfile = CreateBasicProfile("from");

        private readonly CredentialProfile _toProfile = CreateBasicProfile("to");

        private static CredentialProfile CreateBasicProfile(string name)
        {
            return new CredentialProfile(name, new CredentialProfileOptions()
            {
                AccessKey = $"access_key_{name}",
                SecretKey = $"secret_key_{name}"
            });
        }

        private void AssertRaisesChanged(Action testCode)
        {
            Assert.Raises<EventArgs>(
                handler => _sut.Changed += handler,
                handler => _sut.Changed -= handler,
                testCode);
        }

        public MemoryCredentialsFileTests()
        {
            _sut = new MemoryCredentialsFile();

            // Sanity check
            Assert.Empty(_sut.ListProfileNames());
            Assert.Empty(_sut.ListProfiles());
        }

        [Fact]
        public void CopyProfileFromNonExistingProfileFails()
        {
            Assert.Throws<ArgumentException>(() => _sut.CopyProfile(_fromProfile.Name, _toProfile.Name));
        }

        [Fact]
        public void CopyProfileFromExistingProfileToNonExistingProfileSucceeds()
        {
            _sut.RegisterProfile(_fromProfile);

            AssertRaisesChanged(() => _sut.CopyProfile(_fromProfile.Name, _toProfile.Name));

            Assert.True(_sut.TryGetProfile(_fromProfile.Name, out _));
            Assert.True(_sut.TryGetProfile(_toProfile.Name, out _));
        }

        [Fact]
        public void ForcedCopyProfileFromExistingProfileToExistingProfileSucceeds()
        {
            _sut.RegisterProfile(_fromProfile);
            _sut.RegisterProfile(_toProfile);

            AssertRaisesChanged(() => _sut.CopyProfile(_fromProfile.Name, _toProfile.Name, true));

            Assert.True(_sut.TryGetProfile(_fromProfile.Name, out var fetchedFromProfile));
            Assert.True(_sut.TryGetProfile(_toProfile.Name, out var fetchedToProfile));

            Assert.Equal("access_key_from", fetchedFromProfile.Options.AccessKey);
            Assert.Equal("access_key_from", fetchedToProfile.Options.AccessKey);
        }

        [Fact]
        public void UnforcedCopyProfileFromExistingProfileToExistingProfileFails()
        {
            _sut.RegisterProfile(_fromProfile);
            _sut.RegisterProfile(_toProfile);

            Assert.Throws<ArgumentException>(() => _sut.CopyProfile(_fromProfile.Name, _toProfile.Name, false));
        }

        [Fact]
        public void RenameProfileFromNonExistingProfileFails()
        {
            Assert.Throws<ArgumentException>(() => _sut.RenameProfile(_fromProfile.Name, _toProfile.Name));
        }

        [Fact]
        public void RenameProfileFromExistingProfileToNonExistingProfileSucceeds()
        {
            _sut.RegisterProfile(_fromProfile);

            AssertRaisesChanged(() => _sut.RenameProfile(_fromProfile.Name, _toProfile.Name));

            Assert.False(_sut.TryGetProfile(_fromProfile.Name, out _));
            Assert.True(_sut.TryGetProfile(_toProfile.Name, out _));
        }

        [Fact]
        public void ForcedRenameProfileFromExistingProfileToExistingProfileSucceeds()
        {
            _sut.RegisterProfile(_fromProfile);
            _sut.RegisterProfile(_toProfile);

            AssertRaisesChanged(() => _sut.RenameProfile(_fromProfile.Name, _toProfile.Name, true));

            Assert.False(_sut.TryGetProfile(_fromProfile.Name, out _));
            Assert.True(_sut.TryGetProfile(_toProfile.Name, out var fetchedToProfile));

            Assert.Equal("access_key_from", fetchedToProfile.Options.AccessKey);
        }

        [Fact]
        public void UnforcedRenameProfileFromExistingProfileToExistingProfileFails()
        {
            _sut.RegisterProfile(_fromProfile);
            _sut.RegisterProfile(_toProfile);

            Assert.Throws<ArgumentException>(() => _sut.RenameProfile(_fromProfile.Name, _toProfile.Name, false));
        }

        [Fact]
        public void ListProfileNamesReturnsListOfRegisteredProfileNames()
        {
            _sut.RegisterProfile(_fromProfile);
            _sut.RegisterProfile(_toProfile);

            var profileNames = _sut.ListProfileNames();

            Assert.Equal(2, profileNames.Count);
            Assert.Contains(profileNames, name => _fromProfile.Name == name);
            Assert.Contains(profileNames, name => _toProfile.Name == name);
        }

        [Fact]
        public void ListProfilesReturnsListOfRegisteredProfiles()
        {
            _sut.RegisterProfile(_fromProfile);
            _sut.RegisterProfile(_toProfile);

            var profiles = _sut.ListProfiles();

            Assert.Equal(2, profiles.Count);
            Assert.Contains(profiles, profile => _fromProfile.Equals(profile));
            Assert.Contains(profiles, profile => _toProfile.Equals(profile));
        }

        [Fact]
        public void TryGetProfileReturnsTrueOnExistingProfile()
        {
            _sut.RegisterProfile(_fromProfile);

            Assert.True(_sut.TryGetProfile(_fromProfile.Name, out var fetchedFromProfile));
            Assert.Equal(_fromProfile, fetchedFromProfile);
        }

        [Fact]
        public void TryGetProfileReturnsFalseOnNonExistingProfile()
        {
            Assert.False(_sut.TryGetProfile(_fromProfile.Name, out _));
        }

        [Fact]
        public void RegisterProfileWithInvalidProfileFails()
        {
            // Invalid as no credential profile options are set
            var invalidProfile = new CredentialProfile("invalid", new CredentialProfileOptions());

            Assert.Throws<ArgumentException>(() => _sut.RegisterProfile(invalidProfile));
        }

        [Fact]
        public void RegisterProfileWithExistingProfileNameOverwrites()
        {
            var duplicateProfile = CreateBasicProfile(_fromProfile.Name);
            duplicateProfile.Options.AccessKey = "totally_different";

            _sut.RegisterProfile(_fromProfile);

            Assert.True(_sut.TryGetProfile(_fromProfile.Name, out var fetchedProfile));
            Assert.Equal(_fromProfile.Options.AccessKey, fetchedProfile.Options.AccessKey);

            _sut.RegisterProfile(duplicateProfile);

            Assert.True(_sut.TryGetProfile(_fromProfile.Name, out fetchedProfile));
            Assert.Equal(duplicateProfile.Options.AccessKey, fetchedProfile.Options.AccessKey);
        }

        [Fact]
        public void UnregisterProfileWithExistingProfileSucceeds()
        {
            _sut.RegisterProfile(_fromProfile);

            Assert.True(_sut.TryGetProfile(_fromProfile.Name, out _));

            _sut.UnregisterProfile(_fromProfile.Name);

            Assert.False(_sut.TryGetProfile(_fromProfile.Name, out _));
       }

        [Fact]
        public void UnregisterProfileWithNonExistingProfileSucceeds()
        {
            Assert.False(_sut.TryGetProfile(_fromProfile.Name, out _));

            _sut.UnregisterProfile(_fromProfile.Name);

            Assert.False(_sut.TryGetProfile(_fromProfile.Name, out _));
        }
    }
}
