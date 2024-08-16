﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Treachery.Server;

#nullable disable

namespace Treachery.Server.Migrations
{
    [DbContext(typeof(TreacheryContext))]
    [Migration("20240816150934_UpdatedPersistedGame")]
    partial class UpdatedPersistedGame
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.7");

            modelBuilder.Entity("Treachery.Server.ArchivedGame", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("CreatorUserId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("GameParticipation")
                        .HasColumnType("TEXT");

                    b.Property<string>("GameState")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("ArchivedGames");
                });

            modelBuilder.Entity("Treachery.Server.PersistedGame", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<bool>("BotsArePaused")
                        .HasColumnType("INTEGER");

                    b.Property<DateTimeOffset>("CreationDate")
                        .HasColumnType("TEXT");

                    b.Property<int>("CreatorUserId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("GameId")
                        .HasMaxLength(36)
                        .HasColumnType("TEXT");

                    b.Property<string>("GameName")
                        .HasMaxLength(128)
                        .HasColumnType("TEXT");

                    b.Property<string>("GameParticipation")
                        .HasColumnType("TEXT");

                    b.Property<string>("GameState")
                        .HasColumnType("TEXT");

                    b.Property<string>("HashedPassword")
                        .HasMaxLength(4000)
                        .HasColumnType("TEXT");

                    b.Property<DateTimeOffset>("LastAsyncPlayMessageSent")
                        .HasColumnType("TEXT");

                    b.Property<bool>("ObserversRequirePassword")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("StatisticsSent")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("PersistedGames");
                });

            modelBuilder.Entity("Treachery.Server.User", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Email")
                        .HasMaxLength(4000)
                        .HasColumnType("TEXT");

                    b.Property<string>("HashedPassword")
                        .HasMaxLength(4000)
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .HasMaxLength(4000)
                        .HasColumnType("TEXT");

                    b.Property<string>("PasswordResetToken")
                        .HasMaxLength(4000)
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("PasswordResetTokenCreated")
                        .HasColumnType("TEXT");

                    b.Property<string>("PlayerName")
                        .HasMaxLength(4000)
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Users");
                });
#pragma warning restore 612, 618
        }
    }
}
