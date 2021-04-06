# WAES Technical Assignment

The API objective is to receibe two base64 encoded jsons strings with a identifier through two endpoint and get the comparison details of the two strings through another endpoint.

## The Assignment

The assignment says that the endpoints to put the information should accepts JSON base64 encoded binary data.
The assumption is that the information should be the string resulting from a conversion from a json string to base64.
In this case, the json string `{"key_123": "value_abc"}` will generate a enconded string like `eyJrZXlfMTIzIjogInZhbHVlX2FiYyJ9`

### The Assignment Detailed Requirements

- Provide 2 http endpoints that accepts JSON base64 encoded binary data on both endpoints
    - <host>/v1/diff/<ID>/left and <host>/v1/diff/<ID>/right
- The provided data needs to be diff-ed and the results shall be available on a third end point
    - <host>/v1/diff/<ID>
- The results shall provide the following info in JSON format
    - If equal return that
    - If not of equal size just return that
    - If of same size provide insight in where the diffs are, actual diffs are not needed.
        - So mainly offsets + length in the data
				
## Usage

1. Send a Http **PUT** request to the endpoint `(server:port)/v1/diff/{id}/left`, informing the Id in the Uri and a Base64 Enconded string as a **RequestBody** parameter type with média type `application/json`.
2. Send a Http **PUT** request to the endpoint `(server:port)/v1/diff/{id}/right`, informing the Id in the Uri and a Base64 Enconded string as a **RequestBody** parameter typewith média type `application/json`.
3. Send a Http **GET** request to the endpoint `(server:port)/v1/diff/{id}`, informing the Id in the Uri.

## Implementation details

The storage is only in memory.
The repository class ComparableEncodedDataRepositoryInMemory implements the contract IComparableEncodedDataRepository. Then, it can easilly be replaced by another storage implementation.