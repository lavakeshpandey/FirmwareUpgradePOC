CREATE PROCEDURE Inventory.usp_AddSoftwarePackage(@Swpackage VARBINARY(MAX),@Swhelpdoc VARBINARY(MAX),@SwPkgUID UNIQUEIDENTIFIER,@SwAddedDate DATETIME,@SwPkgVersion VARCHAR (100),@SwPkgDescription NVARCHAR (500),@SwColorStandardID	INT,
													@SwFileDetailsUID UNIQUEIDENTIFIER,@SwFileName NVARCHAR (200),@SwFileFormat NVARCHAR (30),@SwFileSize BIGINT,@SwFileURL NVARCHAR (512),@SwFileUploadDate DATETIME2,@SwFileChecksum VARCHAR (120),
													 @SwFileChecksumType VARCHAR (60), @SwCreatedBy NVARCHAR (200), @SwManufacturer VARCHAR (50), @SwDeviceType VARCHAR (50), @SwModels VARCHAR(MAX),
													@HdFileDetailsUID UNIQUEIDENTIFIER,@HdFileName NVARCHAR (200),@HdFileFormat NVARCHAR (30),@HdFileSize BIGINT,
													@BlobUID UNIQUEIDENTIFIER, @BlobDescription VARCHAR(15))



AS BEGIN
	SET NOCOUNT ON
	SET XACT_ABORT ON

	DECLARE @BlobUidSwPackg AS UNIQUEIDENTIFIER = NEWID(),
		@BlobUidHelpDoc AS UNIQUEIDENTIFIER = NEWID();
	DECLARE @SwVersion AS INT;
	DECLARE @id UNIQUEIDENTIFIER;

	SELECT @SwVersion = Inventory.CheckExistingSwPackage(@SwFileName, @SwPkgVersion);

		 BEGIN TRANSACTION
			
			IF @SwVersion IS NOT NULL
				BEGIN 
				-- Set the IsDelted to 1 for existing record(s)
				SELECT @id =(SWPKG.SwPkgUID) FROM Inventory.SoftwarePackage AS SWPKG
				INNER JOIN
				Inventory.FileDetails AS FD
				ON SWPKG.SwPkgUID = FD.SwPkgUID
				WHERE SWPKG.SwPkgVersion = @SwPkgVersion AND FD.FileName = @SwFileName

				UPDATE Inventory.SoftwarePackage SET IsDeleted = 1 WHERE SoftwarePackage.SwPkgUID = @id;

				--Add SoftwarePackage with @SwVersion +1 
				INSERT INTO [Inventory].[SoftwarePackage]
           ([SwPkgUID] ,[SwPkgDescription],[SwPkgVersion],[Manufacturer],[DeviceType],[SwColorStandardID],[AddedDate],[SwVersion],[ReleaseDate],[IsMajor],[IsObsolete],[IsSupportSwUpdate],[IsSupportPwUpdate],[IsSupportNTP],[StoreInDB],[IsDeleted])
				VALUES
           (@SwPkgUID,@SwPkgDescription, @SwPkgVersion,@SwManufacturer, @SwDeviceType, @SwColorStandardID,@SwAddedDate,@SwVersion +1 ,null,null,null,null,null,null,null,0)
				END
			ELSE 
				BEGIN
				
				-- Make a fresh entry for software the package
				INSERT INTO [Inventory].[SoftwarePackage]
           ([SwPkgUID] ,[SwPkgDescription],[SwPkgVersion],[Manufacturer],[DeviceType],[SwColorStandardID],[AddedDate],[SwVersion],[ReleaseDate],[IsMajor],[IsObsolete],[IsSupportSwUpdate],[IsSupportPwUpdate],[IsSupportNTP],[StoreInDB],[IsDeleted])
				VALUES
           (@SwPkgUID,@SwPkgDescription, @SwPkgVersion,@SwManufacturer, @SwDeviceType,@SwColorStandardID,@SwAddedDate,1 ,null,null,null,null,null,null,null,0)
				END
			
		  --File details software package
		   INSERT INTO [Inventory].[FileDetails]([FileDetailsUID],[SwPkgUID],[ParentDirectory],[FileName],[FileFormat],[FileSize],[FileURL],[FileUploadDate],[FileChecksum],[FileChecksumType],[CreatedBy])
     VALUES
           (@SwFileDetailsUID,@SwPkgUID, null,  @SwFileName, @SwFileFormat, @SwFileSize, @SwFileURL, @SwFileUploadDate, @SwFileChecksum, @SwFileChecksumType, @SwCreatedBy)

		   --Software package
		   INSERT INTO [Inventory].[Blobs]([BlobUID],[Description],[BlobTypeUID],[Blob])
     VALUES
           (@BlobUidSwPackg, @BlobDescription, '151C28A2-7B47-4764-85AE-940B3901BA97', @Swpackage)

		     ---SW package blobs map
		   INSERT INTO [Inventory].[SwPackageBlobsMap]([BlobUID],[SwPkgUID])
     VALUES
           ( @BlobUidSwPackg, @SwPkgUID)
		   ---

		   ----File details HelpDoc
		   IF @HdFileSize > 0 
			  BEGIN
					   INSERT INTO [Inventory].[FileDetails]([FileDetailsUID],[SwPkgUID],[ParentDirectory],[FileName],[FileFormat],[FileSize],[FileURL],[FileUploadDate],[FileChecksum],[FileChecksumType],[CreatedBy])
				 VALUES
					   (@HdFileDetailsUID, @SwPkgUID,null, @HdFileName ,@HdFileFormat ,@HdFileSize , @SwFileURL, @SwFileUploadDate, @SwFileChecksum, @SwFileChecksumType, @SwCreatedBy)
			  
			           --Help doc
					   INSERT INTO [Inventory].[Blobs]([BlobUID],[Description],[BlobTypeUID],[Blob])
				 VALUES
					   (@BlobUidHelpDoc, @BlobDescription, 'B1CD7C3D-DA88-4608-834B-86F388813D8C', @Swhelpdoc)
			  
						  --Map 
					  INSERT INTO [Inventory].[SwPackageBlobsMap]([BlobUID],[SwPkgUID])
				 VALUES
					   ( @BlobUidHelpDoc, @SwPkgUID)
			  
			  END

		   
		   --Insert into SWModelMap

			INSERT INTO [Inventory].[SwPackageModelMap]([Manufacturer] ,[DeviceModelName] ,[SwPkgUID])
			SELECT @SwManufacturer, value, @SwPkgUID FROM STRING_SPLIT(@SwModels,',')
			
		COMMIT
END