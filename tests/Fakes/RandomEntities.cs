using System;
using Bogus;
using SolidTUS.Models;

namespace SolidTUS.Tests.Fakes;

public static class RandomEntities
{
    public static UploadFileInfo UploadFileInfo()
    {
        var faker = new Faker<UploadFileInfo>();

        faker.RuleFor(f => f.ByteOffset, f => f.Random.Long(0));
        faker.RuleFor(f => f.FileSize, f => f.Random.Long(0).OrNull(f, 0.5f));

        return faker.Generate();
    }
}
