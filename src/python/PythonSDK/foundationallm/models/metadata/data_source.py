from typing import Optional, Union
from foundationallm.langchain.data_sources.csv import CSVConfiguration
from foundationallm.langchain.data_sources.sql import SQLDatabaseConfiguration
from foundationallm.langchain.data_sources.blob import BlobStorageConfiguration
from foundationallm.langchain.data_sources.cxo import CXOConfiguration
from foundationallm.langchain.data_sources.search_service import SearchServiceConfiguration
from .metadata_base import MetadataBase

class DataSource(MetadataBase):
    """Data source metadata model."""
    configuration: Union[
        CSVConfiguration,
        SQLDatabaseConfiguration,
        CXOConfiguration,
        BlobStorageConfiguration,
        SearchServiceConfiguration
        ]
    data_description: Optional[str] = None
