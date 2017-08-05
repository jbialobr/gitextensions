using System.IO;
using FluentAssertions;
using Gravatar;
using GravatarTests.Properties;
using NSubstitute;
using NUnit.Framework;

namespace GravatarTests
{
    [TestFixture]
    public class GravatarServiceTests
    {
        private const string Email = "x@x.com";
        private IImageCache _cache;
        private GravatarService _service;

        [SetUp]
        public void Setup()
        {
            _cache = Substitute.For<IImageCache>();

            _service = new GravatarService(_cache);
        }

        [Test]
        public async void GetAvatarAsync_should_not_call_gravatar_if_exist_in_cache()
        {
            var avatar = Resources.User;
            _cache.GetImageAsync(Arg.Any<string>(), null).Returns(avatar);

            var image = await _service.GetAvatarAsync(Email, 1, DefaultImageType.Identicon);

            image.Should().Be(avatar);
            Received.InOrder(async () =>
            {
                await _cache.Received(1).GetImageAsync($"{Email}.png", null);
            });
            await _cache.DidNotReceive().AddImageAsync(Arg.Any<string>(), Arg.Any<Stream>());
        }

        [Ignore("Need to abstract WebClient or replace with HttpClient")]
        public void GetAvatarAsync_should_call_gravatar_if_absent_from_cache()
        {
            //_service.ConfigureCache("", 1);
            //var avatar = Resources.User;
            //_cache.GetImageAsync(Arg.Any<string>(), null).Returns(_ => null, _ => avatar);

            //var image = await _service.GetAvatarAsync(Email, 1, DefaultImageType.Identicon);

            //image.Should().Be(avatar);
            //Received.InOrder(async () =>
            //{
            //    await _cache.Received(1).GetImageAsync($"{Email}.png", null);
            //    await _cache.Received(1).AddImageAsync($"{Email}.png", Arg.Any<Stream>());
            //    await _cache.Received(1).GetImageAsync($"{Email}.png", null);
            //});
        }

        [Test]
        public async void RemoveAvatarAsync_should_invoke_cache_remove()
        {
            await _service.DeleteAvatarAsync(Email);

            Received.InOrder(async () =>
            {
                await _cache.Received(1).DeleteImageAsync($"{Email}.png");
            });
        }
    }
}
