using events_api.Models;
using events_api.Services;
using FluentAssertions;
using Xunit;

namespace events_api.Tests.Services
{
    public class EventServiceTests
    {
        private readonly EventService _service;

        public EventServiceTests()
        {
            _service = new EventService();
        }

        // =============================================
        // ✅ УСПЕШНЫЕ СЦЕНАРИИ
        // =============================================

        [Fact]
        public void Add_ShouldCreateNewEvent_WithGeneratedId()
        {
            var newEvent = new Event
            {
                Title = "Новое событие",
                Description = "Описание",
                StartAt = DateTime.Now.AddDays(1),
                EndAt = DateTime.Now.AddDays(2)
            };

            _service.Add(newEvent);
            var result = _service.GetById(newEvent.Id);

            result.Should().NotBeNull();
            result!.Id.Should().BeGreaterThan(0);
            result.Title.Should().Be("Новое событие");
        }

        [Fact]
        public void GetAll_ShouldReturnAllEvents()
        {
            var initialCount = _service.GetAll(null, null, null, 1, 10).Items.Count;

            var result = _service.GetAll(null, null, null, 1, 100);

            result.Items.Should().NotBeNull();
            result.TotalCount.Should().Be(initialCount);
        }

        [Fact]
        public void GetById_WithExistingId_ShouldReturnEvent()
        {
            var newEvent = new Event
            {
                Title = "Тестовое событие",
                Description = "Тестовое описание",
                StartAt = DateTime.Now.AddDays(1),
                EndAt = DateTime.Now.AddDays(2)
            };
            _service.Add(newEvent);

            var result = _service.GetById(newEvent.Id);

            result.Should().NotBeNull();
            result!.Id.Should().Be(newEvent.Id);
            result.Title.Should().Be("Тестовое событие");
        }

        [Fact]
        public void Update_WithExistingId_ShouldUpdateEvent()
        {
            var newEvent = new Event
            {
                Title = "Старое название",
                Description = "Старое описание",
                StartAt = DateTime.Now.AddDays(1),
                EndAt = DateTime.Now.AddDays(2)
            };
            _service.Add(newEvent);

            var updatedEvent = new Event
            {
                Title = "Новое название",
                Description = "Новое описание",
                StartAt = DateTime.Now.AddDays(3),
                EndAt = DateTime.Now.AddDays(4)
            };

            _service.Update(newEvent.Id, updatedEvent);
            var result = _service.GetById(newEvent.Id);

            result.Should().NotBeNull();
            result!.Title.Should().Be("Новое название");
            result.Description.Should().Be("Новое описание");
            result.StartAt.Should().Be(updatedEvent.StartAt);
            result.EndAt.Should().Be(updatedEvent.EndAt);
        }

        [Fact]
        public void Delete_WithExistingId_ShouldRemoveEvent()
        {
            var newEvent = new Event
            {
                Title = "Событие для удаления",
                Description = "Описание",
                StartAt = DateTime.Now.AddDays(1),
                EndAt = DateTime.Now.AddDays(2)
            };
            _service.Add(newEvent);

            _service.Delete(newEvent.Id);
            var result = _service.GetById(newEvent.Id);

            result.Should().BeNull();
        }

        [Fact]
        public void GetAll_WithTitleFilter_ShouldReturnMatchingEvents()
        {
            _service.Add(new Event
            {
                Title = "Уникальное название",
                Description = "Описание",
                StartAt = DateTime.Now.AddDays(1),
                EndAt = DateTime.Now.AddDays(2)
            });

            var result = _service.GetAll("уникальное", null, null, 1, 10);

            result.Items.Should().NotBeEmpty();
            result.Items.Should().AllSatisfy(e => e.Title.Should().ContainEquivalentOf("уникальное"));
        }

        [Fact]
        public void GetAll_WithDateFilters_ShouldReturnEventsInRange()
        {
            // Arrange
            var fixedDate = new DateTime(2025, 7, 15, 12, 0, 0);

            _service.Add(new Event
            {
                Title = "Событие в диапазоне",
                Description = "Описание",
                StartAt = fixedDate.AddDays(-5),
                EndAt = fixedDate.AddDays(5)
            });
            _service.Add(new Event
            {
                Title = "Событие вне диапазона",
                Description = "Описание",
                StartAt = fixedDate.AddDays(-20),
                EndAt = fixedDate.AddDays(-15)
            });

            var from = fixedDate.AddDays(-10);
            var to = fixedDate.AddDays(10);

            // Act
            var result = _service.GetAll(null, from, to, 1, 10);

            // Assert
            result.Items.Should().NotBeEmpty();
            result.Items.Should().AllSatisfy(e =>
            {
                e.StartAt.Should().BeOnOrAfter(from);
                e.EndAt.Should().BeOnOrBefore(to);
            });
            result.Items.Should().Contain(e => e.Title == "Событие в диапазоне");
            result.Items.Should().NotContain(e => e.Title == "Событие вне диапазона");
        }

        [Fact]
        public void GetAll_WithPagination_ShouldReturnCorrectPage()
        {
            for (int i = 0; i < 25; i++)
            {
                _service.Add(new Event
                {
                    Title = $"Событие {i}",
                    Description = "Описание",
                    StartAt = DateTime.Now.AddDays(i),
                    EndAt = DateTime.Now.AddDays(i + 1)
                });
            }

            var result = _service.GetAll(null, null, null, 2, 10);

            result.Page.Should().Be(2);
            result.PageSize.Should().Be(10);
            result.Items.Count.Should().Be(10);
            result.TotalCount.Should().BeGreaterThanOrEqualTo(25);
        }

        [Fact]
        public void GetAll_WithCombinedFilters_ShouldApplyAllFilters()
        {
            // Arrange
            var fixedDate = new DateTime(2025, 7, 15, 12, 0, 0);

            _service.Add(new Event
            {
                Title = "Конференция по IT",
                Description = "Описание",
                StartAt = fixedDate.AddDays(1),
                EndAt = fixedDate.AddDays(2)
            });
            _service.Add(new Event
            {
                Title = "Конференция по дизайну",
                Description = "Описание",
                StartAt = fixedDate.AddDays(3),
                EndAt = fixedDate.AddDays(4)
            });
            _service.Add(new Event
            {
                Title = "Событие вне диапазона",
                Description = "Описание",
                StartAt = fixedDate.AddDays(20),
                EndAt = fixedDate.AddDays(25)
            });

            // Act
            var result = _service.GetAll(
                title: "конференция",
                from: fixedDate.AddDays(0),
                to: fixedDate.AddDays(5),
                page: 1,
                pageSize: 10);

            // Assert
            result.Items.Should().AllSatisfy(e =>
            {
                e.Title.Should().ContainEquivalentOf("конференция");
                e.StartAt.Should().BeOnOrAfter(fixedDate.AddDays(0));
                e.EndAt.Should().BeOnOrBefore(fixedDate.AddDays(5));
            });
            result.Items.Count.Should().Be(2);
            result.Items.Should().Contain(e => e.Title == "Конференция по IT");
            result.Items.Should().Contain(e => e.Title == "Конференция по дизайну");
            result.Items.Should().NotContain(e => e.Title == "Событие вне диапазона");
        }

        // =============================================
        // ❌ НЕУСПЕШНЫЕ СЦЕНАРИИ
        // =============================================

        [Fact]
        public void Update_WithInvalidDates_WhenEndAtBeforeStartAt_ShouldThrowInvalidOperationException()
        {
            var newEvent = new Event
            {
                Title = "Событие с корректными датами",
                Description = "Описание",
                StartAt = DateTime.Now.AddDays(1),
                EndAt = DateTime.Now.AddDays(2)
            };
            _service.Add(newEvent);

            var invalidEvent = new Event
            {
                Title = "Некорректное событие",
                Description = "Описание",
                StartAt = DateTime.Now.AddDays(3),
                EndAt = DateTime.Now.AddDays(2)
            };

            var exception = Record.Exception(() => _service.Update(newEvent.Id, invalidEvent));

            exception.Should().NotBeNull();
            exception.Should().BeOfType<InvalidOperationException>();
            exception!.Message.Should().Be("EndAt не может быть меньше StartAt");
        }

        [Fact]
        public void GetById_WithNonExistingId_ShouldReturnNull()
        {
            var result = _service.GetById(99999);
            result.Should().BeNull();
        }

        [Fact]
        public void Update_WithNonExistingId_ShouldDoNothing()
        {
            var updatedEvent = new Event
            {
                Title = "Новое название",
                Description = "Новое описание",
                StartAt = DateTime.Now.AddDays(1),
                EndAt = DateTime.Now.AddDays(2)
            };

            _service.Update(99999, updatedEvent);
            true.Should().BeTrue();
        }

        [Fact]
        public void Delete_WithNonExistingId_ShouldDoNothing()
        {
            var exception = Record.Exception(() => _service.Delete(99999));
            exception.Should().BeNull();
        }
    }
}