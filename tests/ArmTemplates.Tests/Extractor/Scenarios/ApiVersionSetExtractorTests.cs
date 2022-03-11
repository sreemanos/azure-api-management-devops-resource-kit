﻿// --------------------------------------------------------------------------
//  <copyright file="ApiVersionSetExtractorTests.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation. All rights reserved.
//  </copyright>
// --------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Commands.Executors;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.API.Clients.ApiVersionSet;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Constants;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.Abstractions;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.ApiVersionSet;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.Builders;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Extractor.EntityExtractors;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Extractor.Models;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Tests.Extractor.Abstractions;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Tests.Moqs.ApiClients;
using Moq;
using Xunit;

namespace Microsoft.Azure.Management.ApiManagement.ArmTemplates.Tests.Extractor.Scenarios
{
    [Trait("Category", "Api Version Set Extraction")]
    public class ApiVersionSetExtractorTests : ExtractorMockerWithOutputTestsBase
    {
        public ApiVersionSetExtractorTests() : base("api-version-set-tests")
        {
        }

        [Fact]
        public async Task GenerateApiVersionSetTemplates_ProperlyLaysTheInformation()
        {
            // arrange
            var currentTestDirectory = Path.Combine(this.OutputDirectory, nameof(GenerateApiVersionSetTemplates_ProperlyLaysTheInformation));

            var extractorConfig = this.GetMockedExtractorConsoleAppConfiguration(
                splitApis: false,
                apiVersionSetName: string.Empty,
                multipleApiNames: string.Empty,
                includeAllRevisions: false);
            var extractorParameters = new ExtractorParameters(extractorConfig);

            var mockedApiVersionSetClient = MockApiVersionSetClient.GetMockedApiClientWithDefaultValues();
            var apiVersionSetExtractor = new ApiVersionSetExtractor(
                this.GetTestLogger<ApiVersionSetExtractor>(), 
                new TemplateBuilder(),
                mockedApiVersionSetClient);

            var extractorExecutor = ExtractorExecutor.BuildExtractorExecutor(
                this.GetTestLogger<ExtractorExecutor>(),
                apiVersionSetExtractor: apiVersionSetExtractor);
            extractorExecutor.SetExtractorParameters(extractorParameters);

            // act
            var apiVersionSetTemplate = await extractorExecutor.GenerateApiVersionSetTemplateAsync(
                singleApiName: It.IsAny<string>(),
                currentTestDirectory,
                armTemplateResources: It.IsAny<List<TemplateResource>>());

            // assert
            File.Exists(Path.Combine(currentTestDirectory, extractorParameters.FileNames.ApiVersionSets)).Should().BeTrue();

            apiVersionSetTemplate.Parameters.Should().ContainKey(ParameterNames.ApimServiceName);
            apiVersionSetTemplate.Resources.Count().Should().Be(2);

            (apiVersionSetTemplate.Resources[0].Name.Contains(MockApiVersionSetClient.ApiVersionSetName1) ||
                apiVersionSetTemplate.Resources[1].Name.Contains(MockApiVersionSetClient.ApiVersionSetName1)).Should().BeTrue();
            (apiVersionSetTemplate.Resources[0].Name.Contains(MockApiVersionSetClient.ApiVersionSetName2) ||
                apiVersionSetTemplate.Resources[1].Name.Contains(MockApiVersionSetClient.ApiVersionSetName2)).Should().BeTrue();

            foreach (var templateResource in apiVersionSetTemplate.Resources)
            {
                var apiVersionSetResource = templateResource as ApiVersionSetTemplateResource;
                apiVersionSetResource.Should().NotBeNull();

                apiVersionSetResource.Type.Should().Be(ResourceTypeConstants.ApiVersionSet);
                apiVersionSetResource.Properties.Should().NotBeNull();

                apiVersionSetResource.Properties.DisplayName.Should().NotBeNullOrEmpty();
                apiVersionSetResource.Properties.Description.Should().NotBeNullOrEmpty();
                apiVersionSetResource.Properties.VersionHeaderName.Should().NotBeNullOrEmpty();
                apiVersionSetResource.Properties.VersioningScheme.Should().NotBeNullOrEmpty();
                apiVersionSetResource.Properties.VersionQueryName.Should().NotBeNullOrEmpty();
            }
        }
    }
}
