# Rock OneMap Plugin
This <a href="https://rockrms.com/">RockRMS</a> plugin adds standardization and geocoding of Singapore addresses from <a href="https://www.onemap.gov.sg/apidocs/">OneMap</a>.

## Getting started
Once the plugin has been installed, head over to `System Settings > Location Service` to enable the OneMap address verification component.

### Recommended set up for Singapore addresses
Before proceeding, ensure  `Support International Addresses` is set to `Y` in `General Settings > Global Attributes`.

Head over to `General Settings > Defined Types` and look for Singapore under `Countries`.

- **Address Format:**
  ```
  {{ Street1 }}
  {{ Street2 }}
  {{ Country }} {{ PostalCode }}
  ```
- **Show Address Line 2:** Yes
- **Address Line 1 Requirement:** Required
- **Address Line 2 Requirement:** Optional
- **City Requirement:** Hidden
- **Locality Requirement:** Hidden
- **State Requirement:** Hidden
- **Postal Code Requirement:** Required

## Address verification
- Verification returns "No match" if the `PostalCode` is empty or if `Country` is not Singapore
- Lookups are based on `Street1`, `Street2` and `PostalCode`
- The unit number is removed from the lookup query as OneMap does not return any result with it
- If the result returns with a postal code that matches the input `PostalCode`, the address is verified
- Where multiple results are returned, the first match is selected

## Standardization
For a verified address,
- `Street1` will be updated with the block number and road name
- `Street2` will be updated with the input unit number extracted before the lookup
- `County`, `City` and `State` will be unset

## Geocoding
OneMap supports geocoding of addresses. For verified addresses, the location point of the address will be updated.
