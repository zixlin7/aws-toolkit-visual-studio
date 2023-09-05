﻿using System;

using Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard;

using Xunit;

namespace AWSToolkit.Tests.CommonUI.CredentialProfiles.AddEditWizard
{
    public class Service1 { }

    public class Service2 { }

    public class UnsetService { }

    // All generic methods delegate to non-generic methods, so only the non-generic
    // methods are tested here.
    public class ServiceProviderTests
    {
        private readonly ServiceProvider _sut;

        private readonly Service1 _service1 = new Service1();
        private readonly Service2 _service2a = new Service2();
        private readonly Service2 _service2b = new Service2();
        private readonly string _service2bName = "service2bName";

        public ServiceProviderTests()
        {
            _sut = new ServiceProvider();

            _sut.SetService(_service1);
            _sut.SetService(_service2a);
            _sut.SetService(_service2b, _service2bName);
        }

        [Fact]
        public void GetServiceReturnsService()
        {
            var type = _sut.GetService(typeof(Service2));
            Assert.Equal(_service2a, type);

            var typeAndName = _sut.GetService(typeof(Service2), _service2bName);
            Assert.Equal(_service2b, typeAndName);
        }

        [Fact]
        public void GetServiceForUnsetServiceReturnsNull()
        {
            var unset = _sut.GetService(typeof(UnsetService));
            Assert.Null(unset);
        }

        [Fact]
        public void GetServiceWhereNotBothCorrectTypeAndNameReturnsNull()
        {
            var rightTypeWrongName = _sut.GetService(typeof(Service2), "wrong name");
            Assert.Null(rightTypeWrongName);

            var wrongTypeRightName = _sut.GetService(typeof(Service1), _service2bName);
            Assert.Null(wrongTypeRightName);
        }

        [Fact]
        public void RequireServiceReturnsService()
        {
            var type = _sut.RequireService(typeof(Service2));
            Assert.Equal(_service2a, type);

            var typeAndName = _sut.RequireService(typeof(Service2), _service2bName);
            Assert.Equal(_service2b, typeAndName);
        }

        [Fact]
        public void RequireServiceForUnsetServiceThrows()
        {
            Assert.Throws<NotImplementedException>(() => _sut.RequireService(typeof(UnsetService)));
        }

        [Fact]
        public void RequireServiceWhereNotBothCorrectTypeAndNameThrows()
        {
            Assert.Throws<NotImplementedException>(() => _sut.RequireService(typeof(Service2), "wrong name"));
            Assert.Throws<NotImplementedException>(() => _sut.RequireService(typeof(Service1), _service2bName));
        }

        [Fact]
        public void SetServiceWithNullServiceThrows()
        {
            // generics used here as type cannot be inferred by compiler when supplying nulls
            Assert.Throws<ArgumentNullException>(() => _sut.SetService<Service1>(null));
            Assert.Throws<ArgumentNullException>(() => _sut.SetService<Service2>(null, _service2bName));
        }

        [Fact]
        public void NullServiceTypeAlwaysFails()
        {
            // Type only
            Assert.Throws<ArgumentNullException>(() => _sut.GetService(null));
            Assert.Throws<ArgumentNullException>(() => _sut.RequireService(null));
            Assert.Throws<ArgumentNullException>(() => _sut.SetService(null, _service1));

            // Type and name
            Assert.Throws<ArgumentNullException>(() => _sut.GetService(null, _service2bName));
            Assert.Throws<ArgumentNullException>(() => _sut.RequireService(null, _service2bName));
            Assert.Throws<ArgumentNullException>(() => _sut.SetService(null, _service1, _service2bName));
        }
    }
}
