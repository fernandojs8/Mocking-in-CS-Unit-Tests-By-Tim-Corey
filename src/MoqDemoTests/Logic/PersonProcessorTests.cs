using System;
using System.Collections.Generic;
using System.Linq;
using Autofac.Extras.Moq;
using DemoLibrary.Logic;
using DemoLibrary.Models;
using DemoLibrary.Utilities;
using Moq;
using Xunit;

namespace MoqDemoTests.Logic
{
    public class PersonProcessorTests
    {
        [Theory]
        [InlineData("6'8\"", true, 80)]
        [InlineData("6\"8'", false, 0)]
        [InlineData("six'eight\"", false, 0)]
        public void ConvertHeightTextToInches_VariousOptions(
            string heightText,
            bool expectedIsValid,
            double expectedHeightInInches)
        {
            PersonProcessor processor = new PersonProcessor(null);

            var actual = processor.ConvertHeightTextToInches(heightText);

            Assert.Equal(expectedIsValid, actual.isValid);
            Assert.Equal(expectedHeightInInches, actual.heightInInches);
        }

        [Theory]
        [InlineData("Tim", "Corey", "6'8\"", 80)]
        [InlineData("Charitry", "Corey", "5'4\"", 64)]
        public void CreatePerson_Successful(string firstName, string lastName, string heightText, double expectedHeight)
        {
            PersonProcessor processor = new PersonProcessor(null);

            PersonModel expected = new PersonModel
            {
                FirstName = firstName,
                LastName = lastName,
                HeightInInches = expectedHeight,
                Id = 0
            };

            var actual = processor.CreatePerson(firstName, lastName, heightText);

            Assert.Equal(expected.Id, actual.Id);
            Assert.Equal(expected.FirstName, actual.FirstName);
            Assert.Equal(expected.LastName, actual.LastName);
            Assert.Equal(expected.HeightInInches, actual.HeightInInches);

        }

        [Theory]
        [InlineData("Tim#", "Corey", "6'8\"", "firstName")]
        [InlineData("Charitry", "C88ey", "5'4\"", "lastName")]
        [InlineData("Jon", "Corey", "SixTwo", "heightText")]
        [InlineData("", "Corey", "5'11\"", "firstName")]
        public void CreatePerson_ThrowsException(string firstName, string lastName, string heightText, string expectedInvalidParameter)
        {
            PersonProcessor processor = new PersonProcessor(null);

            var ex = Record.Exception(() => processor.CreatePerson(firstName, lastName, heightText));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentException>(ex);

            if (ex is ArgumentException argEx)
            {
                Assert.Equal(expectedInvalidParameter, argEx.ParamName);
            }
        }

        [Fact]
        public void LoadPerson_LoadsUpPeopleListProperly()
        {
            using (var mock = AutoMock.GetLoose())
            {
                // Arrange
                mock.Mock<ISqliteDataAccess>()
                    .Setup(x => x.LoadData<PersonModel>("select * from Person"))
                    .Returns(GetSamplePeople());

                var personProcessor = mock.Create<PersonProcessor>();
                var expected = GetSamplePeople();

                // Act
                var actual = personProcessor.LoadPeople();

                // Assert
                Assert.True(actual != null);
                Assert.Equal(expected.Count, actual.Count);

                for (var i = 0; i < expected.Count; i++)
                {
                    Assert.Equal(expected[i].FirstName, actual[i].FirstName);
                    Assert.Equal(expected[i].LastName, actual[i].LastName);
                }
            }
        }

        [Fact]
        public void SavePerson_SavesDataProperly()
        {
            using (var mock = AutoMock.GetLoose())
            {
                // Arrange
                var person = new PersonModel()
                {
                    Id = 1,
                    FirstName = "Fernando",
                    LastName = "JS",
                    HeightInInches = 120,
                };

                string sql = "insert into Person (FirstName, LastName, HeightInInches) " +
                "values (@FirstName, @LastName, @HeightInInches)";

                mock.Mock<ISqliteDataAccess>()
                    .Setup(x => 
                        x.SaveData<PersonModel>(
                            person,
                            sql));

                var personProcessor = mock.Create<PersonProcessor>();
                var expected = GetSamplePeople();

                // Act
                personProcessor.SavePerson(person);

                // Assert
                mock.Mock<ISqliteDataAccess>()
                    .Verify(x => x.SaveData(person, sql), Times.Exactly(1));
            }
        }

        [Fact]
        public void UpdatePerson_UpdatesDataProperly()
        {
            using (var mock = AutoMock.GetLoose())
            {
                // Arrange
                var personSave = new PersonModel()
                {
                    Id = 1,
                    FirstName = "Fernando",
                    LastName = "Jesus Santos",
                    HeightInInches = 120,
                };

                
                var personUpdate = new PersonModel()
                {
                    Id = 1,
                    FirstName = "Fernando",
                    LastName = "JS",
                    HeightInInches = 120,
                };

                

                string sqlSave = "insert into Person (FirstName, LastName, HeightInInches) " +
                "values (@FirstName, @LastName, @HeightInInches)";

                string sqlUpdate = "update Person set FirstName = @FirstName, LastName = @LastName" +
                ", HeightInInches = @HeightInInches where Id = @Id";

                mock.Mock<ISqliteDataAccess>()
                    .Setup(x =>
                        x.SaveData<PersonModel>(
                            personSave,
                            sqlSave));

                mock.Mock<ISqliteDataAccess>()
                    .Setup(x =>
                        x.UpdateData(
                            personUpdate,
                            sqlUpdate));

                mock.Mock<ISqliteDataAccess>()
                    .Setup(x => x.LoadData<PersonModel>("select * from Person"))
                    .Returns(new List<PersonModel>() { personUpdate });

                var personProcessor = mock.Create<PersonProcessor>();

                // Act
                personProcessor.SavePerson(personSave);


                personProcessor.UpdatePerson(personUpdate);

                var expected = personProcessor.LoadPeople();

                // Assert
                mock.Mock<ISqliteDataAccess>()
                    .Verify(x => x.UpdateData(personUpdate, sqlUpdate), Times.Exactly(1));

                Assert.Contains(personUpdate, expected);
            }
        }

        private List<PersonModel> GetSamplePeople()
        {
            var output = new List<PersonModel>()
            {
                new PersonModel
                {
                    FirstName = "Fernando",
                    LastName = "JS",
                },
                new PersonModel
                {
                    FirstName = "Tim",
                    LastName = "Corey",
                },
                new PersonModel
                {
                    FirstName = "John",
                    LastName = "Doe",
                }
            };

            return output;
        }
    }
}
