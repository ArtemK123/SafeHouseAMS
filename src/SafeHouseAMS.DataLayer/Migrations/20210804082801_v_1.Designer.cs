﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using SafeHouseAMS.DataLayer;

namespace SafeHouseAMS.DataLayer.Migrations
{
    [DbContext(typeof(DataContext))]
    [Migration("20210804082801_v_1")]
    partial class v_1
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasDefaultSchema("public")
                .HasAnnotation("Relational:MaxIdentifierLength", 63)
                .HasAnnotation("ProductVersion", "5.0.8")
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            modelBuilder.Entity("SafeHouseAMS.DataLayer.Models.LifeSituations.BaseRecordDAL", b =>
                {
                    b.Property<Guid>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasComment("Идентификатор записи");

                    b.Property<string>("Content")
                        .IsRequired()
                        .HasColumnType("jsonb")
                        .HasComment("Содержимое записи в виде json");

                    b.Property<Guid>("DocumentID")
                        .HasColumnType("uuid")
                        .HasComment("Ссылка на документ, породивший запись");

                    b.Property<string>("RecordType")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("ID");

                    b.HasIndex("DocumentID");

                    b.ToTable("Records");

                    b.HasDiscriminator<string>("RecordType").HasValue("BaseRecordDAL");

                    b
                        .HasComment("Записи изменения жизненных ситуаций");
                });

            modelBuilder.Entity("SafeHouseAMS.DataLayer.Models.LifeSituations.LifeSituationDocumentDAL", b =>
                {
                    b.Property<Guid>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasComment("Идентификатор записи - ПК");

                    b.Property<DateTime>("Created")
                        .HasColumnType("timestamp without time zone")
                        .HasComment("Дата-время создания записи");

                    b.Property<DateTime>("DocumentDate")
                        .HasColumnType("timestamp without time zone")
                        .HasComment("Дата документа");

                    b.Property<string>("DocumentType")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<bool>("IsDeleted")
                        .HasColumnType("boolean")
                        .HasComment("Признак удалённой записи");

                    b.Property<DateTime>("LastEdit")
                        .HasColumnType("timestamp without time zone")
                        .HasComment("Дата-время последнего редактирования записи");

                    b.Property<Guid>("SurvivorID")
                        .HasColumnType("uuid")
                        .HasComment("Внешний ключ - пострадавший к которому относится запись");

                    b.HasKey("ID");

                    b.HasIndex("Created");

                    b.HasIndex("DocumentDate");

                    b.HasIndex("IsDeleted");

                    b.HasIndex("LastEdit");

                    b.HasIndex("SurvivorID");

                    b.ToTable("LifeSituationDocuments");

                    b.HasDiscriminator<string>("DocumentType").HasValue("LifeSituationDocumentDAL");

                    b
                        .HasComment("Документы изменения жизненных ситуаций");
                });

            modelBuilder.Entity("SafeHouseAMS.DataLayer.Models.Survivors.SurvivorDAL", b =>
                {
                    b.Property<Guid>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasComment("Идентификатор записи");

                    b.Property<DateTime?>("BirthDateAccurate")
                        .HasColumnType("timestamp without time zone")
                        .HasComment("Точная дата рождения");

                    b.Property<DateTime?>("BirthDateCalculated")
                        .HasColumnType("timestamp without time zone")
                        .HasComment("Вычисленная дата рождения");

                    b.Property<DateTime>("Created")
                        .HasColumnType("timestamp without time zone")
                        .HasComment("Дата создания");

                    b.Property<bool>("IsDeleted")
                        .HasColumnType("boolean")
                        .HasComment("Признак удаленной записи");

                    b.Property<DateTime>("LastEdit")
                        .HasColumnType("timestamp without time zone")
                        .HasComment("Дата последнего редактирования");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasComment("Имя");

                    b.Property<int>("Num")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasComment("Номер карточки")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<string>("OtherSex")
                        .HasColumnType("text")
                        .HasComment("Уточнение пола");

                    b.Property<int>("Sex")
                        .HasColumnType("integer")
                        .HasComment("Пол");

                    b.HasKey("ID");

                    b.ToTable("Survivors");

                    b
                        .HasComment("Пострадавшие");
                });

            modelBuilder.Entity("SafeHouseAMS.DataLayer.Models.LifeSituations.ChildrenRecordDAL", b =>
                {
                    b.HasBaseType("SafeHouseAMS.DataLayer.Models.LifeSituations.BaseRecordDAL");

                    b.HasDiscriminator().HasValue("HasChildren");
                });

            modelBuilder.Entity("SafeHouseAMS.DataLayer.Models.LifeSituations.CitizenshipRecordDAL", b =>
                {
                    b.HasBaseType("SafeHouseAMS.DataLayer.Models.LifeSituations.BaseRecordDAL");

                    b.HasDiscriminator().HasValue("Citizenship");
                });

            modelBuilder.Entity("SafeHouseAMS.DataLayer.Models.LifeSituations.DomicileRecordDAL", b =>
                {
                    b.HasBaseType("SafeHouseAMS.DataLayer.Models.LifeSituations.BaseRecordDAL");

                    b.HasDiscriminator().HasValue("Domicile");
                });

            modelBuilder.Entity("SafeHouseAMS.DataLayer.Models.LifeSituations.EducationLevelRecordDAL", b =>
                {
                    b.HasBaseType("SafeHouseAMS.DataLayer.Models.LifeSituations.BaseRecordDAL");

                    b.HasDiscriminator().HasValue("EducationLevel");
                });

            modelBuilder.Entity("SafeHouseAMS.DataLayer.Models.LifeSituations.SpecialityRecordDAL", b =>
                {
                    b.HasBaseType("SafeHouseAMS.DataLayer.Models.LifeSituations.BaseRecordDAL");

                    b.HasDiscriminator().HasValue("Speciality");
                });

            modelBuilder.Entity("SafeHouseAMS.DataLayer.Models.LifeSituations.InquiryDAL", b =>
                {
                    b.HasBaseType("SafeHouseAMS.DataLayer.Models.LifeSituations.LifeSituationDocumentDAL");

                    b.Property<string>("AddictionKind")
                        .HasColumnType("text")
                        .HasComment("Тип зависимости");

                    b.Property<bool>("ChildhoodViolence")
                        .HasColumnType("boolean")
                        .HasComment("Насилие в детстве");

                    b.Property<string>("ForwardedByOrgannization")
                        .HasColumnType("text")
                        .HasComment("Какая организация направила");

                    b.Property<string>("ForwardedByPerson")
                        .HasColumnType("text")
                        .HasComment("Детали направления другим человеком");

                    b.Property<string>("ForwardedBySurvivor")
                        .HasColumnType("text")
                        .HasComment("Детали направления другим пострадавшим");

                    b.Property<bool>("HasAddiction")
                        .HasColumnType("boolean")
                        .HasComment("Наличие зависимости");

                    b.Property<bool>("HasOtherVulnerability")
                        .HasColumnType("boolean")
                        .HasComment("другие факторы уязвимости");

                    b.Property<int>("HealthStatusVulnerabilityMask")
                        .HasColumnType("integer")
                        .HasComment("битовая маска уязвимости по здоровью");

                    b.Property<bool>("Homelessness")
                        .HasColumnType("boolean")
                        .HasComment("Бездомность");

                    b.Property<bool>("IsForwardedByOrganization")
                        .HasColumnType("boolean")
                        .HasComment("Направлен организацией");

                    b.Property<bool>("IsForwardedByPerson")
                        .HasColumnType("boolean")
                        .HasComment("Перенаправлен другим человеком");

                    b.Property<bool>("IsForwardedBySurvivor")
                        .HasColumnType("boolean")
                        .HasComment("Перенаправлен другим пострадавшим");

                    b.Property<bool>("IsJuvenile")
                        .HasColumnType("boolean")
                        .HasComment("Несовершеннолетний в момент обращения");

                    b.Property<bool>("IsSelfInquiry")
                        .HasColumnType("boolean")
                        .HasComment("Самообращение");

                    b.Property<bool>("Migration")
                        .HasColumnType("boolean")
                        .HasComment("Мигрант_ка");

                    b.Property<bool>("OrphanageExperience")
                        .HasColumnType("boolean")
                        .HasComment("Опыт интернатного учреждения");

                    b.Property<string>("OtherHealthStatusVulnerabilityDetail")
                        .HasColumnType("text")
                        .HasComment("Детали уязвимости по здоровью");

                    b.Property<string>("OtherVulnerabilityDetails")
                        .HasColumnType("text")
                        .HasComment("Описание других факторов уязвимости");

                    b.Property<int?>("SelfInquirySourcesMask")
                        .HasColumnType("integer")
                        .HasComment("Битовая маска каналов самообращения");

                    b.Property<string>("WorkingExperience")
                        .HasColumnType("text")
                        .HasComment("Описание опыта работы");

                    b.HasIndex("ForwardedByOrgannization");

                    b.HasDiscriminator().HasValue("Inquiry");
                });

            modelBuilder.Entity("SafeHouseAMS.DataLayer.Models.LifeSituations.BaseRecordDAL", b =>
                {
                    b.HasOne("SafeHouseAMS.DataLayer.Models.LifeSituations.LifeSituationDocumentDAL", "Document")
                        .WithMany("Records")
                        .HasForeignKey("DocumentID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Document");
                });

            modelBuilder.Entity("SafeHouseAMS.DataLayer.Models.LifeSituations.LifeSituationDocumentDAL", b =>
                {
                    b.HasOne("SafeHouseAMS.DataLayer.Models.Survivors.SurvivorDAL", "Survivor")
                        .WithMany()
                        .HasForeignKey("SurvivorID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Survivor");
                });

            modelBuilder.Entity("SafeHouseAMS.DataLayer.Models.LifeSituations.LifeSituationDocumentDAL", b =>
                {
                    b.Navigation("Records");
                });
#pragma warning restore 612, 618
        }
    }
}
