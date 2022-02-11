﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using calledudeBot.Database;

#nullable disable

namespace calledudeBotCore.Migrations
{
    [DbContext(typeof(DatabaseContext))]
    [Migration("20220121205131_UserActivity_MessagesSent")]
    partial class UserActivity_MessagesSent
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "6.0.1");

            modelBuilder.Entity("calledudeBot.Database.UserActivity.UserActivityEntity", b =>
                {
                    b.Property<string>("Username")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("LastJoinDate")
                        .HasColumnType("TEXT");

                    b.Property<int>("MessagesSent")
                        .HasColumnType("INTEGER");

                    b.Property<int>("TimesSeen")
                        .HasColumnType("INTEGER");

                    b.HasKey("Username");

                    b.ToTable("UserActivities");
                });

            modelBuilder.Entity("calledudeBot.Database.UserSession.UserSessionEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("EndTime")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("StartTime")
                        .HasColumnType("TEXT");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<TimeSpan>("WatchTime")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("UserSession");
                });
#pragma warning restore 612, 618
        }
    }
}
