package com.localmind.fileservice.architecture;

import static com.tngtech.archunit.lang.syntax.ArchRuleDefinition.classes;

import com.tngtech.archunit.core.importer.ImportOption;
import com.tngtech.archunit.junit.AnalyzeClasses;
import com.tngtech.archunit.junit.ArchTest;
import com.tngtech.archunit.lang.ArchRule;

@AnalyzeClasses(
    packages = "com.localmind.fileservice",
    importOptions = ImportOption.DoNotIncludeTests.class)
class ArchitectureTest {
  @ArchTest
  static final ArchRule domainDoesNotDependOnFrameworks =
      classes()
          .that()
          .resideInAPackage("..domain..")
          .should()
          .onlyDependOnClassesThat()
          .resideOutsideOfPackages("org.springframework..", "jakarta.persistence..", "io.minio..");

  @ArchTest
  static final ArchRule controllersDoNotDependOnInfrastructure =
      classes()
          .that()
          .resideInAPackage("..api..")
          .should()
          .onlyDependOnClassesThat()
          .resideOutsideOfPackages("..infrastructure..");
}
