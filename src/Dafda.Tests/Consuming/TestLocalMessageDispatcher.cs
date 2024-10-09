using System;
using System.Threading.Tasks;
using Dafda.Consuming;
using Dafda.Tests.Builders;
using Dafda.Tests.TestDoubles;
using Moq;
using Xunit;

namespace Dafda.Tests.Consuming
{
    public class TestLocalMessageDispatcher
    {
        [Fact]
        public async Task throws_expected_exception_when_dispatching_and_no_handler_has_been_registered()
        {
            var messageResultStub = new MessageResultBuilder().Build();
            var emptyMessageHandlerRegistryStub = new MessageHandlerRegistry();
            var dummyFactory = new HandlerUnitOfWorkFactoryStub(null);

            var sut = new LocalMessageDispatcherBuilder()
                .WithMessageHandlerRegistry(emptyMessageHandlerRegistryStub)
                .WithHandlerUnitOfWorkFactory(dummyFactory)
                .Build();

            await Assert.ThrowsAsync<MissingMessageHandlerRegistrationException>(() => sut.Dispatch(messageResultStub));
        }

        [Fact]
        public async Task throws_expected_exception_when_dispatching_and_unable_to_resolve_handler_instance()
        {
            var messageResultStub = new MessageResultBuilder().WithTransportLevelMessageType("foo").WithTopic("non-existent-topic").Build();
            var messageRegistrationStub = new MessageRegistrationBuilder().WithMessageType("foo").Build();
            var registry = new MessageHandlerRegistry();
            registry.Register(messageRegistrationStub);

            var sut = new LocalMessageDispatcherBuilder()
                .WithMessageHandlerRegistry(registry)
                .WithHandlerUnitOfWorkFactory(new HandlerUnitOfWorkFactoryStub(null))
                .Build();

            await Assert.ThrowsAsync<MissingMessageHandlerRegistrationException>(() => sut.Dispatch(messageResultStub));
        }

        [Fact]
        public async Task handler_is_invoked_as_expected_when_dispatching()
        {
            var mock = new Mock<IMessageHandler<object>>();

            var messageResultStub = new MessageResultBuilder().WithTransportLevelMessageType("foo").WithTopic("topic").Build();
            var registrationDummy = new MessageRegistrationBuilder().WithMessageType("foo").WithTopic("topic").Build();
            var registry = new MessageHandlerRegistry();
            registry.Register(registrationDummy);

            var sut = new LocalMessageDispatcherBuilder()
                .WithMessageHandlerRegistry(registry)
                .WithHandlerUnitOfWork(new UnitOfWorkStub(mock.Object))
                .Build();

            await sut.Dispatch(messageResultStub);

            mock.Verify(x => x.Handle(It.IsAny<object>(), It.IsAny<MessageHandlerContext>()), Times.Once);
        }

        [Fact]
        public async Task handler_exceptions_are_thrown_as_expected()
        {
            var transportMessageDummy = new MessageResultBuilder().WithTransportLevelMessageType("foo").WithTopic("some-topic").Build();
            var registrationDummy = new MessageRegistrationBuilder().WithMessageType("foo").WithTopic("some-other").Build();
            var registry = new MessageHandlerRegistry();
            registry.Register(registrationDummy);

            var sut = new LocalMessageDispatcherBuilder()
                .WithMessageHandlerRegistry(registry)
                .WithHandlerUnitOfWork(new UnitOfWorkStub(new ErroneusHandler()))
                .Build();

            await Assert.ThrowsAsync<MissingMessageHandlerRegistrationException>(() => sut.Dispatch(transportMessageDummy));
        }

        #region private helper classes

        private class ExpectedException : Exception
        {

        }

        private class ErroneusHandler : IMessageHandler<object>
        {
            public Task Handle(object message, MessageHandlerContext context)
            {
                throw new ExpectedException();
            }
        }

        #endregion
    }
}