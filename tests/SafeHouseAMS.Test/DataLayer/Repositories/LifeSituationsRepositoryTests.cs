﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using SafeHouseAMS.BizLayer.LifeSituations;
using SafeHouseAMS.BizLayer.LifeSituations.InquirySources;
using SafeHouseAMS.BizLayer.LifeSituations.Records;
using SafeHouseAMS.BizLayer.LifeSituations.Vulnerabilities;
using SafeHouseAMS.DataLayer;
using SafeHouseAMS.DataLayer.Models.LifeSituations;
using SafeHouseAMS.DataLayer.Repositories;
using Xunit;
using Xunit.Categories;

namespace SafeHouseAMS.Test.DataLayer.Repositories
{
    public class LifeSituationsRepositoryTests
    {
        private IMapper CreateMapper()
        {
            var cfg = new MapperConfiguration(c => c.AddMaps(typeof(SurvivorsRepository).Assembly));
            return new Mapper(cfg);
        }

        private DataContext CreateInMemoryDatabase()
        {
            var connection = new SqliteConnection("Filename=:memory:");
            connection.Open();
            var dbctxOptsBuilder = new DbContextOptionsBuilder()
                .UseLazyLoadingProxies()
                .UseSqlite(connection, opt =>
                    opt.MigrationsAssembly(typeof(DataContext).Assembly.FullName));
            var ctx = new DataContext(dbctxOptsBuilder.Options);
            ctx.Database.EnsureDeleted();
            ctx.Database.EnsureCreated();
            return ctx;
        }

        [Fact,UnitTest]
        public void Ctor_WhenDataContextIsNull_Throws() =>
            Assert.Throws<ArgumentNullException>(() =>
                new LifeSituationDocumentsRepository(null!, Mock.Of<IMapper>()));

        [Fact, UnitTest]
        public void Ctor_WhenMapperIsNull_Throws() =>
            Assert.Throws<ArgumentNullException>(() =>
                new LifeSituationDocumentsRepository(new DataContext(new DbContextOptions<DataContext>()), null!));


        [Fact, IntegrationTest]
        public async Task GetSingleAsync_WhenCalled_ReturnsEntity()
        {
            //arrange
            var id = Guid.NewGuid();
            await using var ctx = CreateInMemoryDatabase();
            var survivorId = Guid.NewGuid();
            await ctx.Survivors.AddAsync(new() {ID = survivorId, Num = 42, Name = "name"});
            await ctx.LifeSituationDocuments.AddAsync(new InquiryDAL {ID = id, SurvivorID = survivorId});
            var citizenshipRecord = new CitizenshipRecord(Guid.NewGuid(), "c");
            await ctx.Records.AddAsync(new CitizenshipRecordDAL {ID = Guid.NewGuid(), Content = JsonSerializer.Serialize(citizenshipRecord), DocumentID = id});
            await ctx.SaveChangesAsync();
            var sut = new LifeSituationDocumentsRepository(ctx, CreateMapper());

            //act
            var foundRecord = await sut.GetSingleAsync(id, CancellationToken.None);

            //assert
            foundRecord.ID.Should().Be(id);
        }

        [Fact, IntegrationTest]
        public async Task GetSingleAsync_WhenRecordIsDeleted_Throws()
        {
            //arrange
            var id = Guid.NewGuid();
            await using var ctx = CreateInMemoryDatabase();

            var survivorId = Guid.NewGuid();
            await ctx.Survivors.AddAsync(new() {ID = survivorId, Num = 42, Name = "name"});
            await ctx.LifeSituationDocuments.AddAsync(new InquiryDAL{ID = id, IsDeleted = true, SurvivorID = survivorId});
            await ctx.SaveChangesAsync();
            var sut = new LifeSituationDocumentsRepository(ctx, CreateMapper());

            //act && assert
            await Assert.ThrowsAnyAsync<Exception>(() => sut.GetSingleAsync(id, CancellationToken.None));
        }

        [Fact, IntegrationTest]
        public async Task GetSingleAsync_WhenCancelled_ThrowsOperationCancelled()
        {
            //arrange
            await using var ctx = CreateInMemoryDatabase();
            var sut = new LifeSituationDocumentsRepository(ctx, CreateMapper());
            var ct = new CancellationToken(true);
            //act && assert
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sut.GetSingleAsync(default, ct));
        }

        [Fact,IntegrationTest]
        public async Task CreateInquiry_WhenCalled_ActuallyAddsNewRecord()
        {
            //arrange
            await using var ctx = CreateInMemoryDatabase();
            var surId = Guid.NewGuid();
            await ctx.Survivors.AddAsync(new() {ID = surId, Num = 42, Name = "ololo"});
            await ctx.SaveChangesAsync();
            var sut = new LifeSituationDocumentsRepository(ctx, CreateMapper());
            var id = Guid.NewGuid();
            var time = DateTime.Now;

            //act
            var inqSources = new List<IInquirySource>
            {
                new SelfInquiry(SelfInquiry.InquiryChannel.Email | SelfInquiry.InquiryChannel.Phone),
                new ForwardedByOrganization("org"),
                new ForwardedByPerson("person"),
                new ForwardedBySurvivor("survivor"),
            };
            await sut.CreateInquiry(id, false, time, time, surId, DateTime.Today, false, inqSources);

            //assert
            var foundRecord = await ctx.LifeSituationDocuments.SingleAsync(x => x.ID == id);

            foundRecord.ID.Should().Be(id);
            foundRecord.IsDeleted.Should().Be(false);
            foundRecord.Created.Should().Be(time);
            foundRecord.LastEdit.Should().Be(time);
            foundRecord.Should().BeOfType<InquiryDAL>();
            var inquiry = foundRecord as InquiryDAL;

            inquiry?.IsForwardedByOrganization.Should().BeTrue();
            inquiry?.ForwardedByOrgannization.Should().Be("org");

            inquiry?.IsForwardedByPerson.Should().BeTrue();
            inquiry?.ForwardedByPerson.Should().Be("person");

            inquiry?.IsForwardedBySurvivor.Should().BeTrue();
            inquiry?.ForwardedBySurvivor.Should().Be("survivor");

            inquiry?.IsSelfInquiry.Should().BeTrue();
            inquiry?.SelfInquirySourcesMask.Should().Be(24);
        }

        [Fact,IntegrationTest]
        public async Task GetAllbySurvivor_WhenCalled_ReturnsOnlyForSelectedSurvivor()
        {
            //arrange
            await using var ctx = CreateInMemoryDatabase();

            var surId1 = Guid.NewGuid();
            var surId2 = Guid.NewGuid();
            await ctx.Survivors.AddAsync(new() {ID = surId1, Num = 42, Name = "ololo"});
            await ctx.Survivors.AddAsync(new() {ID = surId2, Num = 43, Name = "azaza"});

            var docId1 = Guid.NewGuid();
            var docId2 = Guid.NewGuid();
            var docId3 = Guid.NewGuid();
            await ctx.LifeSituationDocuments.AddRangeAsync(
            new InquiryDAL {ID = docId1, SurvivorID = surId1},
            new InquiryDAL {ID = docId2, SurvivorID = surId1},
            new InquiryDAL {ID = docId3, SurvivorID = surId2});

            var mockRecordContent = JsonSerializer.Serialize(new CitizenshipRecord(default, "citi"));
            await ctx.Records.AddRangeAsync(
                new CitizenshipRecordDAL{ID = Guid.NewGuid(), DocumentID = docId1, Content = mockRecordContent},
                new CitizenshipRecordDAL{ID = Guid.NewGuid(), DocumentID = docId2, Content = mockRecordContent});

            await ctx.SaveChangesAsync();
            var sut = new LifeSituationDocumentsRepository(ctx, CreateMapper());

            //act
            var result = new List<LifeSituationDocument>();
            await foreach(var doc in sut.GetAllBySurvivor(surId1, CancellationToken.None))
                result.Add(doc);

            //assert
            result.Should().HaveCount(2);
            result.Should().OnlyContain(x => x.Survivor.ID == surId1);
        }

        [Fact, IntegrationTest]
        public Task AddRecord_WhenCalled_AddsChildrenRecord() =>
            AddRecord_TestCore<ChildrenRecord, ChildrenRecordDAL>(new (Guid.NewGuid(), true, "details"));

        [Fact, IntegrationTest]
        public Task AddRecord_WhenCalled_AddsCitizenshipRecord() =>
            AddRecord_TestCore<CitizenshipRecord, CitizenshipRecordDAL>(new (Guid.NewGuid(), "details"));

        [Fact, IntegrationTest]
        public Task AddRecord_WhenCalled_AddsMigrationStatusRecord() =>
            AddRecord_TestCore<MigrationStatusRecord, MigrationStatusRecordDAL>(new (Guid.NewGuid(), "details"));

        [Fact, IntegrationTest]
        public Task AddRecord_WhenCalled_AddsRegistrationStatusRecord() =>
            AddRecord_TestCore<RegistrationStatusRecord, RegistrationStatusRecordDAL>(new (Guid.NewGuid(), "details"));

        [Fact, IntegrationTest]
        public Task AddRecord_WhenCalled_AddsDomicileRecord() =>
            AddRecord_TestCore<DomicileRecord, DomicileRecordDAL>(new(Guid.NewGuid(), "details",
                DomicileRecord.PlaceKind.Dorm, default, default, default, default,
                default, default, default, default,
                default, default));

        [Fact, IntegrationTest]
        public Task AddRecord_WhenCalled_AddsEducationLevelRecord() =>
            AddRecord_TestCore<EducationLevelRecord, EducationLevelRecordDAL>(new (Guid.NewGuid(), EducationLevelRecord.EduLevel.Courses, "details"));

        [Fact, IntegrationTest]
        public Task AddRecord_WhenCalled_AddsSpecialityRecord() =>
            AddRecord_TestCore<SpecialityRecord, SpecialityRecordDAL>(new (Guid.NewGuid(), "details"));

        private class MockRecord : BaseRecord
        {
            public MockRecord() : base(Guid.NewGuid())
            {
            }
        }
        private class MockRecordDAL : BaseRecordDAL {}

        [Fact, IntegrationTest]
        public Task AddRecord_WhenCalledWithUnknownRecord_Throws() =>
            Assert.ThrowsAsync<ArgumentException>(()=> AddRecord_TestCore<MockRecord, MockRecordDAL>(new ()));

        private async Task AddRecord_TestCore<TRecord, TRecordDAL>(TRecord srcRecord)
            where TRecord : BaseRecord
            where TRecordDAL : BaseRecordDAL
        {
            //arrange
            await using var ctx = CreateInMemoryDatabase();

            var surId1 = Guid.NewGuid();
            await ctx.Survivors.AddAsync(new() {ID = surId1, Num = 42, Name = "ololo"});

            var docId1 = Guid.NewGuid();
            await ctx.LifeSituationDocuments.AddAsync(new InquiryDAL {ID = docId1, SurvivorID = surId1});

            await ctx.SaveChangesAsync();
            var sut = new LifeSituationDocumentsRepository(ctx, CreateMapper());

            //act
            await sut.AddRecord(docId1, srcRecord);

            //assert
            var record = await ctx.Records.SingleAsync(x => x.ID == srcRecord.ID);
            record.Should().BeOfType<TRecordDAL>();
            record.DocumentID.Should().Be(docId1);
        }

        [Fact, IntegrationTest]
        public async Task GetCitizenshipsCompletions_WhenCalled_ReturnsCollectionFromAllRecords()
        {
            //arrange
            await using var ctx = CreateInMemoryDatabase();

            var surId1 = Guid.NewGuid();
            await ctx.Survivors.AddAsync(new() {ID = surId1, Num = 42, Name = "ololo"});

            var docId1 = Guid.NewGuid();
            var docId2 = Guid.NewGuid();
            var docId3 = Guid.NewGuid();
            var docId4 = Guid.NewGuid();
            await ctx.LifeSituationDocuments.AddRangeAsync(
            new InquiryDAL {ID = docId1, SurvivorID = surId1},
            new InquiryDAL {ID = docId2, SurvivorID = surId1},
            new InquiryDAL {ID = docId3, SurvivorID = surId1},
            new InquiryDAL {ID = docId4, SurvivorID = surId1});

            const string c1 = "c1";
            const string c2 = "c2";
            const string jsonPattern = "\"id\":\"00000000-0000-0000-0000-000000000000\", \"Citizenship\":\"{0}\"";

            await ctx.Records.AddRangeAsync(
            new CitizenshipRecordDAL{ID = Guid.NewGuid(), DocumentID = docId2,
                Content = string.Concat("{", string.Format(jsonPattern, c2), "}")},
            new CitizenshipRecordDAL{ID = Guid.NewGuid(), DocumentID = docId1,
                Content = string.Concat("{", string.Format(jsonPattern, c1), "}")},
            new CitizenshipRecordDAL{ID = Guid.NewGuid(), DocumentID = docId2,
                Content = string.Concat("{", string.Format(jsonPattern, c1), "}")},
            new CitizenshipRecordDAL{ID = Guid.NewGuid(), DocumentID = docId3,
                Content = string.Concat("{", string.Format(jsonPattern, c1), "}")},
            new CitizenshipRecordDAL{ID = Guid.NewGuid(), DocumentID = docId4,
                Content = string.Concat("{", string.Format(jsonPattern, c2), "}")});

            await ctx.SaveChangesAsync();
            var sut = new LifeSituationDocumentsRepository(ctx, CreateMapper());

            //act
            var result = new List<string>();
            await foreach(var autoCompleteHint in sut.GetCitizenshipsCompletions(CancellationToken.None))
                result.Add(autoCompleteHint);

            //assert
            result.Should().HaveCount(2);
            result.Should().ContainInOrder(c1, c2);
        }

        [Fact,IntegrationTest]
        public async Task SetWorkingExperience_WhenCalled_SavesWorkingExperienceInInquiry()
        {
            //arrange
            await using var ctx = CreateInMemoryDatabase();

            var surId1 = Guid.NewGuid();
            await ctx.Survivors.AddAsync(new() {ID = surId1, Num = 42, Name = "ololo"});

            var docId = Guid.NewGuid();
            await ctx.LifeSituationDocuments.AddAsync(new InquiryDAL {ID = docId, SurvivorID = surId1});
            await ctx.SaveChangesAsync();

            var sut = new LifeSituationDocumentsRepository(ctx, CreateMapper());
            const string workingExperience = "details";
            //act

            await sut.SetWorkingExperience(docId, workingExperience);

            //assert
            var document = await ctx.LifeSituationDocuments
                .OfType<InquiryDAL>()
                .SingleAsync(x => x.ID == docId);
            document.WorkingExperience.Should().Be(workingExperience);
        }

        [Fact,IntegrationTest]
        public async Task SetAddiction_WhenCalled_SetsAddictionInInquiry()
        {
            //arrange
            await using var ctx = CreateInMemoryDatabase();

            var surId1 = Guid.NewGuid();
            await ctx.Survivors.AddAsync(new() {ID = surId1, Num = 42, Name = "ololo"});

            var docId = Guid.NewGuid();
            await ctx.LifeSituationDocuments.AddAsync(new InquiryDAL {ID = docId, SurvivorID = surId1});
            await ctx.SaveChangesAsync();

            var sut = new LifeSituationDocumentsRepository(ctx, CreateMapper());
            const string addictionKind = "details";
            //act

            await sut.SetAddiction(docId, addictionKind);

            //assert
            var document = await ctx.LifeSituationDocuments
                .OfType<InquiryDAL>()
                .SingleAsync(x => x.ID == docId);
            document.HasAddiction.Should().BeTrue();
            document.AddictionKind.Should().Be(addictionKind);
        }

        [Fact,IntegrationTest]
        public async Task ClearAddiction_WhenCalled_ClearsAddictionInInquiry()
        {
            //arrange
            await using var ctx = CreateInMemoryDatabase();

            var surId1 = Guid.NewGuid();
            await ctx.Survivors.AddAsync(new() {ID = surId1, Num = 42, Name = "ololo"});

            var docId = Guid.NewGuid();
            await ctx.LifeSituationDocuments.AddAsync(new InquiryDAL {ID = docId, SurvivorID = surId1, AddictionKind = "addiction", HasAddiction = true});
            await ctx.SaveChangesAsync();

            var sut = new LifeSituationDocumentsRepository(ctx, CreateMapper());
            //act

            await sut.ClearAddiction(docId);

            //assert
            var document = await ctx.LifeSituationDocuments
                .OfType<InquiryDAL>()
                .SingleAsync(x => x.ID == docId);
            document.HasAddiction.Should().BeFalse();
            document.AddictionKind.Should().BeNull();
        }

        [Fact,IntegrationTest]
        public async Task SetHomeless_WhenCalled_SetsHomelessInInquiry()
        {
            //arrange
            await using var ctx = CreateInMemoryDatabase();

            var surId1 = Guid.NewGuid();
            await ctx.Survivors.AddAsync(new() {ID = surId1, Num = 42, Name = "ololo"});

            var docId = Guid.NewGuid();
            await ctx.LifeSituationDocuments.AddAsync(new InquiryDAL {ID = docId, SurvivorID = surId1});
            await ctx.SaveChangesAsync();

            var sut = new LifeSituationDocumentsRepository(ctx, CreateMapper());
            //act

            await sut.SetHomeless(docId);

            //assert
            var document = await ctx.LifeSituationDocuments
                .OfType<InquiryDAL>()
                .SingleAsync(x => x.ID == docId);
            document.Homelessness.Should().BeTrue();
        }

        [Fact,IntegrationTest]
        public async Task ClearHomeless_WhenCalled_ClearsHomelessInInquiry()
        {
            //arrange
            await using var ctx = CreateInMemoryDatabase();

            var surId1 = Guid.NewGuid();
            await ctx.Survivors.AddAsync(new() {ID = surId1, Num = 42, Name = "ololo"});

            var docId = Guid.NewGuid();
            await ctx.LifeSituationDocuments.AddAsync(new InquiryDAL {ID = docId, SurvivorID = surId1, Homelessness = true});
            await ctx.SaveChangesAsync();

            var sut = new LifeSituationDocumentsRepository(ctx, CreateMapper());
            //act

            await sut.ClearHomeless(docId);

            //assert
            var document = await ctx.LifeSituationDocuments
                .OfType<InquiryDAL>()
                .SingleAsync(x => x.ID == docId);
            document.Homelessness.Should().BeFalse();
        }

        [Fact,IntegrationTest]
        public async Task SetMigration_WhenCalled_SetsMigrationInInquiry()
        {
            //arrange
            await using var ctx = CreateInMemoryDatabase();

            var surId1 = Guid.NewGuid();
            await ctx.Survivors.AddAsync(new() {ID = surId1, Num = 42, Name = "ololo"});

            var docId = Guid.NewGuid();
            await ctx.LifeSituationDocuments.AddAsync(new InquiryDAL {ID = docId, SurvivorID = surId1});
            await ctx.SaveChangesAsync();

            var sut = new LifeSituationDocumentsRepository(ctx, CreateMapper());
            //act

            await sut.SetMigration(docId);

            //assert
            var document = await ctx.LifeSituationDocuments
                .OfType<InquiryDAL>()
                .SingleAsync(x => x.ID == docId);
            document.Migration.Should().BeTrue();
        }

        [Fact,IntegrationTest]
        public async Task ClearMigration_WhenCalled_ClearsMigrationInInquiry()
        {
            //arrange
            await using var ctx = CreateInMemoryDatabase();

            var surId1 = Guid.NewGuid();
            await ctx.Survivors.AddAsync(new() {ID = surId1, Num = 42, Name = "ololo"});

            var docId = Guid.NewGuid();
            await ctx.LifeSituationDocuments.AddAsync(new InquiryDAL {ID = docId, SurvivorID = surId1, Migration = true});
            await ctx.SaveChangesAsync();

            var sut = new LifeSituationDocumentsRepository(ctx, CreateMapper());
            //act

            await sut.ClearMigration(docId);

            //assert
            var document = await ctx.LifeSituationDocuments
                .OfType<InquiryDAL>()
                .SingleAsync(x => x.ID == docId);
            document.Migration.Should().BeFalse();
        }

        [Fact,IntegrationTest]
        public async Task SetChildhoodViolence_WhenCalled_SetsChildhoodViolenceInInquiry()
        {
            //arrange
            await using var ctx = CreateInMemoryDatabase();

            var surId1 = Guid.NewGuid();
            await ctx.Survivors.AddAsync(new() {ID = surId1, Num = 42, Name = "ololo"});

            var docId = Guid.NewGuid();
            await ctx.LifeSituationDocuments.AddAsync(new InquiryDAL {ID = docId, SurvivorID = surId1});
            await ctx.SaveChangesAsync();

            var sut = new LifeSituationDocumentsRepository(ctx, CreateMapper());
            //act

            await sut.SetChildhoodViolence(docId);

            //assert
            var document = await ctx.LifeSituationDocuments
                .OfType<InquiryDAL>()
                .SingleAsync(x => x.ID == docId);
            document.ChildhoodViolence.Should().BeTrue();
        }

        [Fact,IntegrationTest]
        public async Task ClearChildhoodViolence_WhenCalled_ClearsChildhoodViolenceInInquiry()
        {
            //arrange
            await using var ctx = CreateInMemoryDatabase();

            var surId1 = Guid.NewGuid();
            await ctx.Survivors.AddAsync(new() {ID = surId1, Num = 42, Name = "ololo"});

            var docId = Guid.NewGuid();
            await ctx.LifeSituationDocuments.AddAsync(new InquiryDAL {ID = docId, SurvivorID = surId1, ChildhoodViolence = true});
            await ctx.SaveChangesAsync();

            var sut = new LifeSituationDocumentsRepository(ctx, CreateMapper());
            //act

            await sut.ClearChildhoodViolence(docId);

            //assert
            var document = await ctx.LifeSituationDocuments
                .OfType<InquiryDAL>()
                .SingleAsync(x => x.ID == docId);
            document.ChildhoodViolence.Should().BeFalse();
        }

        [Fact,IntegrationTest]
        public async Task SetOrphanageExperience_WhenCalled_SetsOrphanageExperienceInInquiry()
        {
            //arrange
            await using var ctx = CreateInMemoryDatabase();

            var surId1 = Guid.NewGuid();
            await ctx.Survivors.AddAsync(new() {ID = surId1, Num = 42, Name = "ololo"});

            var docId = Guid.NewGuid();
            await ctx.LifeSituationDocuments.AddAsync(new InquiryDAL {ID = docId, SurvivorID = surId1});
            await ctx.SaveChangesAsync();

            var sut = new LifeSituationDocumentsRepository(ctx, CreateMapper());
            //act

            await sut.SetOrphanageExperience(docId);

            //assert
            var document = await ctx.LifeSituationDocuments
                .OfType<InquiryDAL>()
                .SingleAsync(x => x.ID == docId);
            document.OrphanageExperience.Should().BeTrue();
        }

        [Fact,IntegrationTest]
        public async Task ClearOrphanageExperience_WhenCalled_ClearsOrphanageExperienceInInquiry()
        {
            //arrange
            await using var ctx = CreateInMemoryDatabase();

            var surId1 = Guid.NewGuid();
            await ctx.Survivors.AddAsync(new() {ID = surId1, Num = 42, Name = "ololo"});

            var docId = Guid.NewGuid();
            await ctx.LifeSituationDocuments.AddAsync(new InquiryDAL {ID = docId, SurvivorID = surId1, OrphanageExperience = true});
            await ctx.SaveChangesAsync();

            var sut = new LifeSituationDocumentsRepository(ctx, CreateMapper());
            //act

            await sut.ClearOrphanageExperience(docId);

            //assert
            var document = await ctx.LifeSituationDocuments
                .OfType<InquiryDAL>()
                .SingleAsync(x => x.ID == docId);
            document.OrphanageExperience.Should().BeFalse();
        }

        [Fact,IntegrationTest]
        public async Task SetOther_WhenCalled_SetsOtherInInquiry()
        {
            //arrange
            await using var ctx = CreateInMemoryDatabase();

            var surId1 = Guid.NewGuid();
            await ctx.Survivors.AddAsync(new() {ID = surId1, Num = 42, Name = "ololo"});

            var docId = Guid.NewGuid();
            await ctx.LifeSituationDocuments.AddAsync(new InquiryDAL {ID = docId, SurvivorID = surId1});
            await ctx.SaveChangesAsync();

            var sut = new LifeSituationDocumentsRepository(ctx, CreateMapper());
            const string otherDetails = "other details";

            //act
            await sut.SetOther(docId, otherDetails);

            //assert
            var document = await ctx.LifeSituationDocuments
                .OfType<InquiryDAL>()
                .SingleAsync(x => x.ID == docId);
            document.HasOtherVulnerability.Should().BeTrue();
            document.OtherVulnerabilityDetails.Should().Be(otherDetails);
        }

        [Fact,IntegrationTest]
        public async Task ClearOther_WhenCalled_ClearsOtherInInquiry()
        {
            //arrange
            await using var ctx = CreateInMemoryDatabase();

            var surId1 = Guid.NewGuid();
            await ctx.Survivors.AddAsync(new() {ID = surId1, Num = 42, Name = "ololo"});

            var docId = Guid.NewGuid();
            await ctx.LifeSituationDocuments.AddAsync(new InquiryDAL {ID = docId, SurvivorID = surId1, HasOtherVulnerability = true, OtherVulnerabilityDetails = "other details"});
            await ctx.SaveChangesAsync();

            var sut = new LifeSituationDocumentsRepository(ctx, CreateMapper());
            //act

            await sut.ClearOther(docId);

            //assert
            var document = await ctx.LifeSituationDocuments
                .OfType<InquiryDAL>()
                .SingleAsync(x => x.ID == docId);
            document.HasOtherVulnerability.Should().BeFalse();
            document.OtherVulnerabilityDetails.Should().BeNull();
        }

        [Fact,IntegrationTest]
        public async Task SetHealthStatus_WhenCalled_SetsHealthStatusInInquiry()
        {
            //arrange
            await using var ctx = CreateInMemoryDatabase();

            var surId1 = Guid.NewGuid();
            await ctx.Survivors.AddAsync(new() {ID = surId1, Num = 42, Name = "ololo"});

            var docId = Guid.NewGuid();
            await ctx.LifeSituationDocuments.AddAsync(new InquiryDAL {ID = docId, SurvivorID = surId1});
            await ctx.SaveChangesAsync();

            var sut = new LifeSituationDocumentsRepository(ctx, CreateMapper());
            const string HealthStatusDetails = "HealthStatus details";

            //act
            await sut.SetHealthStatusVulnerability(docId, HealthStatus.HealthStatusVulnerabilityType.Other, HealthStatusDetails);

            //assert
            var document = await ctx.LifeSituationDocuments
                .OfType<InquiryDAL>()
                .SingleAsync(x => x.ID == docId);
            document.HealthStatusVulnerabilityMask.Should().Be(128);
            document.OtherHealthStatusVulnerabilityDetail.Should().Be(HealthStatusDetails);
        }

        [Fact,IntegrationTest]
        public async Task ClearHealthStatus_WhenCalled_ClearsHealthStatusInInquiry()
        {
            //arrange
            await using var ctx = CreateInMemoryDatabase();

            var surId1 = Guid.NewGuid();
            await ctx.Survivors.AddAsync(new() {ID = surId1, Num = 42, Name = "ololo"});

            var docId = Guid.NewGuid();
            await ctx.LifeSituationDocuments.AddAsync(new InquiryDAL {ID = docId, SurvivorID = surId1, HealthStatusVulnerabilityMask = 128, OtherHealthStatusVulnerabilityDetail = "HealthStatus details"});
            await ctx.SaveChangesAsync();

            var sut = new LifeSituationDocumentsRepository(ctx, CreateMapper());
            //act

            await sut.ClearHealthStatusVulnerability(docId);

            //assert
            var document = await ctx.LifeSituationDocuments
                .OfType<InquiryDAL>()
                .SingleAsync(x => x.ID == docId);
            document.HealthStatusVulnerabilityMask.Should().Be(0);
            document.OtherHealthStatusVulnerabilityDetail.Should().BeNull();
        }

        [Fact,IntegrationTest]
        public async Task CreateCitizenshipChange_WhenCalled_AddsNewDocument()
        {
            //arrange
            await using var ctx = CreateInMemoryDatabase();

            var surId1 = Guid.NewGuid();
            await ctx.Survivors.AddAsync(new() {ID = surId1, Num = 42, Name = "ololo"});
            await ctx.SaveChangesAsync();

            var sut = new LifeSituationDocumentsRepository(ctx, CreateMapper());

            var docId = Guid.NewGuid();
            var created = DateTime.Now - TimeSpan.FromDays(5);
            var lastedit = DateTime.Now - TimeSpan.FromDays(4);
            var docdate = DateTime.Now - TimeSpan.FromDays(6);

            //act
            await sut.CreateRecordUpdateDocument(docId, false, created, lastedit, surId1, docdate);

            //assert
            var document = await ctx.LifeSituationDocuments
                .OfType<CitizenshipChangeDAL>()
                .SingleAsync(x => x.ID == docId);

            document.ID.Should().Be(docId);
            document.IsDeleted.Should().BeFalse();
            document.Created.Should().Be(created);
            document.LastEdit.Should().Be(lastedit);
            document.DocumentDate.Should().Be(docdate);
            document.SurvivorID.Should().Be(surId1);
        }
    }
}
