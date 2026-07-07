using events_api.events_api.Exceptions;
using events_api.Models;
using events_api.Services;
using FluentAssertions;
using Xunit;

namespace events_api.Tests.Services
{
    public class EventServiceTests
    {
        // =============================================
        // ✅ УСПЕШНЫЕ СЦЕНАРИИ
        // =============================================

        [Fact]
        public void Add_ShouldCreateNewEvent_WithGeneratedId()
        {
            var service = new EventService();
            var newEvent = new Event
            {
                Title = "Новое событие",
                Description = "Описание",
                StartAt = DateTime.Now.AddDays(1),
                EndAt = DateTime.Now.AddDays(2)
            };

            service.Add(newEvent);
            var result = service.GetById(newEvent.Id);

            result.Should().NotBeNull();
            result!.Id.Should().BeGreaterThan(0);
            result.Title.Should().Be("Новое событие");
        }

        [Fact]
        public void GetAll_ShouldReturnAllEvents()
        {
            var service = new EventService();
            var initialCount = service.GetAll(null, null, null, 1, 10).Items.Count;

            var result = service.GetAll(null, null, null, 1, 100);

            result.Items.Should().NotBeNull();
            result.TotalCount.Should().Be(initialCount);
        }

        [Fact]
        public void GetById_WithExistingId_ShouldReturnEvent()
        {
            var service = new EventService();
            var newEvent = new Event
            {
                Title = "Тестовое событие",
                Description = "Тестовое описание",
                StartAt = DateTime.Now.AddDays(1),
                EndAt = DateTime.Now.AddDays(2)
            };
            service.Add(newEvent);

            var result = service.GetById(newEvent.Id);

            result.Should().NotBeNull();
            result!.Id.Should().Be(newEvent.Id);
            result.Title.Should().Be("Тестовое событие");
        }

        [Fact]
        public void Update_WithExistingId_ShouldUpdateEvent()
        {
            var service = new EventService();
            var newEvent = new Event
            {
                Title = "Старое название",
                Description = "Старое описание",
                StartAt = DateTime.Now.AddDays(1),
                EndAt = DateTime.Now.AddDays(2)
            };
            service.Add(newEvent);

            var updatedEvent = new Event
            {
                Title = "Новое название",
                Description = "Новое описание",
                StartAt = DateTime.Now.AddDays(3),
                EndAt = DateTime.Now.AddDays(4)
            };

            service.Update(newEvent.Id, updatedEvent);
            var result = service.GetById(newEvent.Id);

            result.Should().NotBeNull();
            result!.Title.Should().Be("Новое название");
            result.Description.Should().Be("Новое описание");
            result.StartAt.Should().Be(updatedEvent.StartAt);
            result.EndAt.Should().Be(updatedEvent.EndAt);
        }

        [Fact]
        public void Delete_WithExistingId_ShouldRemoveEvent()
        {
            var service = new EventService();
            var newEvent = new Event
            {
                Title = "Событие для удаления",
                Description = "Описание",
                StartAt = DateTime.Now.AddDays(1),
                EndAt = DateTime.Now.AddDays(2)
            };
            service.Add(newEvent);

            service.Delete(newEvent.Id);
            var result = service.GetById(newEvent.Id);

            result.Should().BeNull();
        }

        [Fact]
        public void GetAll_WithTitleFilter_ShouldReturnMatchingEvents()
        {
            var service = new EventService();
            service.Add(new Event
            {
                Title = "Уникальное название",
                Description = "Описание",
                StartAt = DateTime.Now.AddDays(1),
                EndAt = DateTime.Now.AddDays(2)
            });

            var result = service.GetAll("уникальное", null, null, 1, 10);

            result.Items.Should().NotBeEmpty();
            result.Items.Should().AllSatisfy(e => e.Title.Should().ContainEquivalentOf("уникальное"));
        }

        [Fact]
        public void GetAll_WithDateFilters_ShouldReturnEventsInRange()
        {
            // Arrange
            var service = new EventService();
            var baseDate = new DateTime(2025, 7, 15);

            service.Add(new Event
            {
                Title = "Событие в диапазоне",
                Description = "Описание",
                StartAt = baseDate.AddDays(-5),
                EndAt = baseDate.AddDays(5)
            });
            service.Add(new Event
            {
                Title = "Событие вне диапазона",
                Description = "Описание",
                StartAt = baseDate.AddDays(-20),
                EndAt = baseDate.AddDays(-15)
            });

            var from = baseDate.AddDays(-10);
            var to = baseDate.AddDays(10);

            // Act
            var result = service.GetAll(null, from, to, 1, 10);

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
            var service = new EventService();
            for (int i = 0; i < 25; i++)
            {
                service.Add(new Event
                {
                    Title = $"Событие {i}",
                    Description = "Описание",
                    StartAt = DateTime.Now.AddDays(i),
                    EndAt = DateTime.Now.AddDays(i + 1)
                });
            }

            var result = service.GetAll(null, null, null, 2, 10);

            result.Page.Should().Be(2);
            result.PageSize.Should().Be(10);
            result.Items.Count.Should().Be(10);
            result.TotalCount.Should().BeGreaterThanOrEqualTo(25);
        }

        [Fact]
        public void GetAll_WithCombinedFilters_ShouldApplyAllFilters()
        {
            // Arrange
            var service = new EventService();
            var baseDate = new DateTime(2025, 7, 15);

            service.Add(new Event
            {
                Title = "Конференция по IT",
                Description = "Описание",
                StartAt = baseDate.AddDays(1),
                EndAt = baseDate.AddDays(2)
            });
            service.Add(new Event
            {
                Title = "Конференция по дизайну",
                Description = "Описание",
                StartAt = baseDate.AddDays(3),
                EndAt = baseDate.AddDays(4)
            });
            service.Add(new Event
            {
                Title = "Событие вне диапазона",
                Description = "Описание",
                StartAt = baseDate.AddDays(20),
                EndAt = baseDate.AddDays(25)
            });

            // Act
            var result = service.GetAll(
                title: "конференция",
                from: baseDate.AddDays(0),
                to: baseDate.AddDays(5),
                page: 1,
                pageSize: 10);

            // Assert
            result.Items.Should().NotBeEmpty();
            result.Items.Should().AllSatisfy(e =>
            {
                e.Title.Should().ContainEquivalentOf("конференция");
                e.StartAt.Should().BeOnOrAfter(baseDate.AddDays(0));
                e.EndAt.Should().BeOnOrBefore(baseDate.AddDays(5));
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
        public void Update_WithInvalidDates_WhenEndAtBeforeStartAt_ShouldThrowBusinessException()
        {
            // Arrange
            var service = new EventService();
            var newEvent = new Event
            {
                Title = "Событие с корректными датами",
                Description = "Описание",
                StartAt = DateTime.Now.AddDays(1),
                EndAt = DateTime.Now.AddDays(2)
            };
            service.Add(newEvent);

            var invalidEvent = new Event
            {
                Title = "Некорректное событие",
                Description = "Описание",
                StartAt = DateTime.Now.AddDays(3),
                EndAt = DateTime.Now.AddDays(2) // EndAt раньше StartAt
            };

            // Act & Assert
            var exception = Record.Exception(() => service.Update(newEvent.Id, invalidEvent));

            exception.Should().NotBeNull();
            exception.Should().BeOfType<BusinessException>();
            exception!.Message.Should().Be("EndAt должен быть позже StartAt");
        }

        [Fact]
        public void GetById_WithNonExistingId_ShouldReturnNull()
        {
            var service = new EventService();
            var result = service.GetById(99999);
            result.Should().BeNull();
        }

        [Fact]
        public void Update_WithNonExistingId_ShouldThrowBusinessException()
        {
            // Arrange
            var service = new EventService();
            var nonExistingId = 99999;
            var updatedEvent = new Event
            {
                Title = "Новое название",
                Description = "Новое описание",
                StartAt = DateTime.Now.AddDays(1),
                EndAt = DateTime.Now.AddDays(2)
            };

            // Act & Assert
            var exception = Record.Exception(() => service.Update(nonExistingId, updatedEvent));

            exception.Should().NotBeNull();
            exception.Should().BeOfType<BusinessException>();
            exception!.Message.Should().Be($"Событие с id {nonExistingId} не найдено");
        }

        [Fact]
        public void Delete_WithNonExistingId_ShouldThrowBusinessException()
        {
            // Arrange
            var service = new EventService();
            var nonExistingId = 99999;

            // Act & Assert
            var exception = Record.Exception(() => service.Delete(nonExistingId));

            exception.Should().NotBeNull();
            exception.Should().BeOfType<BusinessException>();
            exception!.Message.Should().Be($"Событие с id {nonExistingId} не найдено");
        }
    }
}