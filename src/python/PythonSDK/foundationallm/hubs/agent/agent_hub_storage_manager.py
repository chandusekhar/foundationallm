"""
Storage manager for the AgentHub.
"""
from typing import List
from foundationallm.config import Configuration
from foundationallm.storage import BlobStorageManager

class AgentHubStorageManager(BlobStorageManager):
    """
    The AgentHubStorageManager class is responsible for fetching
    available agent values from Azure Blob Storage.
    """
    def __init__(self, config: Configuration = None):
        """
        Initialize an agent hub blob storage manager.

        Parameters
        ----------
        config : Configuration
            A reference to the application configuration settings.
        """
        connection_string = config.get_value("FoundationaLLM:AgentHub:StorageManager:BlobStorage:ConnectionString")
        container_name = config.get_value("FoundationaLLM:AgentHub:AgentMetadata:StorageContainer")

        super().__init__(
            blob_connection_string=connection_string,
            container_name=container_name
        )

    def read_file_content(self, path) -> str:
        """
        Retrieves the contents of a specified file.

        Parameters
        ----------
        path : str
            The path to the blob being retrieved.

        Returns
        -------
        str
            Returns the contents of a file as a string.
        """
        file_content = super().read_file_content(path)
        if file_content is not None:
            return file_content.decode()

    def list_blobs(self, path):
        """
        Retrieves a list of blobs in the specified path. The path should be to a folder, not a file.

        Parameters
        ----------
        path : str
            The path to the target blobs.

        Returns
        -------
        List[str]
            A list of blobs in the specified path.
        """
        blob_list: List[dict] = list(super().list_blobs(path=path))
        blob_names = [blob["name"].split('/')[-1] for blob in blob_list]
        return blob_names
