
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Lambda.Util;
using Amazon.Lambda;
using Amazon.Lambda.Model;

using Moq;

using Xunit;

namespace AWSToolkit.Tests.Lambda
{
    public class LambdaStateWaiterTests
    {
        private readonly Mock<IAmazonLambda> _lambda = new Mock<IAmazonLambda>();
        private readonly LambdaStateWaiter _sut;

        private readonly GetFunctionConfigurationResponse _getFunctionConfigurationResponseFirst = new GetFunctionConfigurationResponse();
        private readonly GetFunctionConfigurationResponse _getFunctionConfigurationResponse = new GetFunctionConfigurationResponse();

        public LambdaStateWaiterTests()
        {
            SetupGetFunctionConfiguration();

            _sut = new LambdaStateWaiter(_lambda.Object);
        }

        private void SetupGetFunctionConfiguration()
        {
            _lambda.SetupSequence(mock => mock.GetFunctionConfigurationAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => _getFunctionConfigurationResponseFirst)
                .ReturnsAsync(() => _getFunctionConfigurationResponse);
        }

        [Theory]
        [MemberData(nameof(CreateLambdaStateCombinations))]
        public async Task WaitForUpdatableStateAsync(State initialState, LastUpdateStatus initialUpdateStatus, int expectedTimesCalled)
        {
            _getFunctionConfigurationResponseFirst.State = initialState;
            _getFunctionConfigurationResponseFirst.LastUpdateStatus = initialUpdateStatus;
            _getFunctionConfigurationResponse.State = State.Active;
            _getFunctionConfigurationResponse.LastUpdateStatus = LastUpdateStatus.Successful;

            await _sut.WaitForUpdatableStateAsync("lambda-function-name");
            _lambda.Verify(mock => mock.GetFunctionConfigurationAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(expectedTimesCalled));
        }

        public static IEnumerable<object[]> CreateLambdaStateCombinations()
        {
            var states = new List<State>()
            {
                State.Active,
                State.Failed,
                State.Inactive,
                State.Pending,
            };

            var statuses = new List<LastUpdateStatus>()
            {
                LastUpdateStatus.Successful,
                LastUpdateStatus.Failed,
                LastUpdateStatus.InProgress,
            };

            return from state in states
                   from status in statuses
                   select new object[] { state, status, IsUpdatable(state, status) ? 1 : 2 };
        }

        private static bool IsUpdatable(State state, LastUpdateStatus status)
        {
            return state != State.Pending && status != LastUpdateStatus.InProgress;
        }
    }
}
