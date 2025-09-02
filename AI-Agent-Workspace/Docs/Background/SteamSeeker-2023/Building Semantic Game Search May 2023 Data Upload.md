### May 2023 Data Upload
#### Imports

```python
#Imports
import csv
import json
import os
import pandas as pd
import numpy as np
import re
import tiktoken
import openai
import time
import random
import matplotlib.pyplot as plt
from datetime import datetime
from scipy.stats.mstats import gmean
from pymilvus import connections, FieldSchema, CollectionSchema, DataType, Collection, utility
```

#### Constants

```python
# Constants
GAME_COLLECTION_NAME = 'steam_metatadata_db'  # Collection name
REVIEW_COLLECTION_NAME = 'steam_review_db'  # Collection name
DIMENSION = 1536  # Embeddings size
COUNT = 50000  # Max titles to embed and insert.
URI = os.environ.get("ZILLIZ_URI")  # Endpoint URI obtained from Zilliz Cloud
USER = os.environ.get("ZILLIZ_USERNAME")  # Username specified when you created this database
PASSWORD = os.environ.get("ZILLIZ_PASS")  # Password set for that account
OPENAI_ENGINE = 'text-embedding-ada-002'  # Which engine to use
openai.api_key = os.environ.get("OPENAI_API_KEY")  
```
#### Data Import

```python
db_generation_folder = "../../DB-Generation/"
raw_reviews_folder = os.path.join(db_generation_folder, "Downloaded_Reviews/")
raw_metadata_folder = os.path.join(db_generation_folder, "Downloaded_Metadata/")
raw_app_metadata_file = os.path.join(raw_metadata_folder, "AllAppsMetadata.csv")
processed_db_objects_folder = os.path.join(db_generation_folder, "Processed/")
processed_reviews_folder = os.path.join(processed_db_objects_folder, "Reviews/")
processed_metadata_folder = os.path.join(processed_db_objects_folder, "Metadata/")
```

```python
app_metadata = pd.read_csv(raw_app_metadata_file, dtype=str)
app_metadata['categories'] = app_metadata['categories'].apply(lambda x: str(x).replace('\'', "\"")) # Replace single quotes with double quotes for easier parsing.
```

```python
known_category_enums = pd.read_csv(os.path.join(db_generation_folder, "UsedSteamTags.csv"), dtype={'Category ID:': int, 'Category Name:': str})
known_category_enums
```
```
    Category ID                    Category Name
0             1                     Multi-player
1             2                    Single-player
2             9                            Co-op
3            13               Captions available
4            18       Partial Controller Support
5            19                             Mods
6            20                              MMO
7            22               Steam Achievements
8            23                      Steam Cloud
9            24              Shared/Split Screen
10           27       Cross-Platform Multiplayer
11           28          Full controller support
12           30                   Steam Workshop
13           31                       VR Support
14           33  Native Steam Controller Support
15           35                 In-App Purchases
16           36                       Online PvP
17           37          Shared/Split Screen PvP
18           38                     Online Co-op
19           39        Shared/Split Screen Co-op
20           41             Remote Play on Phone
21           42            Remote Play on Tablet
22           43                Remote Play on TV
23           44             Remote Play Together
24           47                          LAN PvP
25           48                        LAN Co-op
26           49                              PvP
27           51                   Steam Workshop
28           52       Tracked Controller Support
29           53                     VR Supported
30           54                          VR Only
```

```python
known_review_languages = set([
    'latam',
    'vietnamese',
    'dutch',
    'norwegian',
    'romanian',
    'danish',
    'russian',
    'japanese',
    'portuguese',
    'ukrainian',
    'french',
    'italian',
    'bulgarian',
    'polish',
    'turkish',
    'brazilian',
    'german',
    'hungarian',
    'schinese',
    'czech',
    'swedish',
    'koreana',
    'tchinese',
    'greek',
    'spanish',
    'thai',
    'finnish',
    'english'
])
```

```python
app_metadata
```
```
       type                                               name steam_appid   
0      game                                      Slave's Sword      893010  \
1      game                                        VR Invaders      561360   
2       dlc  Resident Evil 4 Weapon Exclusive Upgrade Ticke...     2197320   
3       dlc                           Broforce: The Soundtrack      410790   
4      game                                    My Step Sisters     1526700   
...     ...                                                ...         ...   
79496  game                                             CYBRID     1636850   
79497  game                          Big City Rigs: Bus Driver     1317800   
79498  game                                  The Youthdrainers      393230   
79499  game                                Elva the Eco Dragon     1276370   
79500  game                                        Fated Souls      370780   

      required_age is_free                               detailed_description   
0                0   False  <h1>Latest Release</h1><p><a href="https://sto...  \
1                0   False  <h1>Check out our upcoming game</h1><p><a href...   
2               17   False  To gun enthusiasts, knife collectors, and love...   
3                0   False  Broforce: The Soundtrack by Deon van Heerden<b...   
4                0   False  My step sisters - erotic visual novel, which a...   
...            ...     ...                                                ...   
79496            0   False  <h1>JOIN THE COMMUNITY ON DISCORD</h1><p><a hr...   
79497            0   False  In Big City Rig Bus Driver your job is easy — ...   
79498            0   False  <img src="https://cdn.akamai.steamstatic.com/s...   
79499            0   False  <h2 class="bb_tag">Elva the Eco Dragon, learni...   
79500            0   False  <h2 class="bb_tag">Description</h2><br>Formerl...   

                                          about_the_game   
0      <a href="https://steamcommunity.com/linkfilter...  \
1      VR Invaders is a story-driven sci-fi arcade sh...   
2      To gun enthusiasts, knife collectors, and love...   
3      Broforce: The Soundtrack by Deon van Heerden<b...   
4      My step sisters - erotic visual novel, which a...   
...                                                  ...   
79496  CYBRID is the action-rhythm VR game of the Cyb...   
79497  In Big City Rig Bus Driver your job is easy — ...   
79498  <img src="https://cdn.akamai.steamstatic.com/s...   
79499  <h2 class="bb_tag">Elva the Eco Dragon, learni...   
79500  <h2 class="bb_tag">Description</h2><br>Formerl...   

                                       short_description   
0      Slave's Sword is the story of Luna, a former a...  \
1      Dive into a virtual sci-fi world as the freela...   
2      Here's your ticket to the gun show! Specifical...   
3      The single greatest Broforce soundtrack of all...   
4      My step sisters - erotic visual novel, which a...   
...                                                  ...   
79496  CYBRID is the action-rhythm VR of the Cyber Fu...   
79497  You've got one job — drive a bus! Get behind t...   
79498  In an unknown location, pregnant women are bei...   
79499  &quot;Elva, the Eco Dragon&quot; is a 3D game ...   
79500  Explore magical lands and discover your inner ...   

                                     supported_languages   
0      English, Japanese<strong>*</strong>, Simplifie...  \
1      English<strong>*</strong><br><strong>*</strong...   
2      English<strong>*</strong>, French<strong>*</st...   
3      English<strong>*</strong><br><strong>*</strong...   
4                                       English, Russian   
...                                                  ...   
79496  English<strong>*</strong><br><strong>*</strong...   
79497                                            English   
79498  English<strong>*</strong><br><strong>*</strong...   
79499  English<strong>*</strong>, Spanish - Spain<str...   
79500                                            English   

                                            header_image  ...   
0      https://cdn.akamai.steamstatic.com/steam/apps/...  ...  \
1      https://cdn.akamai.steamstatic.com/steam/apps/...  ...   
2      https://cdn.akamai.steamstatic.com/steam/apps/...  ...   
3      https://cdn.akamai.steamstatic.com/steam/apps/...  ...   
4      https://cdn.akamai.steamstatic.com/steam/apps/...  ...   
...                                                  ...  ...   
79496  https://cdn.akamai.steamstatic.com/steam/apps/...  ...   
79497  https://cdn.akamai.steamstatic.com/steam/apps/...  ...   
79498  https://cdn.akamai.steamstatic.com/steam/apps/...  ...   
79499  https://cdn.akamai.steamstatic.com/steam/apps/...  ...   
79500  https://cdn.akamai.steamstatic.com/steam/apps/...  ...   

                                achievements.highlighted pc_requirements   
0                                                    NaN             NaN  \
1                                                    NaN             NaN   
2                                                    NaN             NaN   
3                                                    NaN             NaN   
4      [{'name': 'Lili is full wet', 'path': 'https:/...             NaN   
...                                                  ...             ...   
79496                                                NaN             NaN   
79497                                                NaN             NaN   
79498  [{'name': 'Battery', 'path': 'https://cdn.akam...             NaN   
79499  [{'name': 'Bronze Leaf', 'path': 'https://cdn....             NaN   
79500  [{'name': '100 gold found', 'path': 'https://c...             NaN   

      ext_user_account_notice  dlc metacritic.score metacritic.url demos   
0                         NaN  NaN              NaN            NaN   NaN  \
1                         NaN  NaN              NaN            NaN   NaN   
2                         NaN  NaN              NaN            NaN   NaN   
3                         NaN  NaN              NaN            NaN   NaN   
4                         NaN  NaN              NaN            NaN   NaN   
...                       ...  ...              ...            ...   ...   
79496                     NaN  NaN              NaN            NaN   NaN   
79497                     NaN  NaN              NaN            NaN   NaN   
79498                     NaN  NaN              NaN            NaN   NaN   
79499                     NaN  NaN              NaN            NaN   NaN   
79500                     NaN  NaN              NaN            NaN   NaN   

      price_overview.recurring_sub price_overview.recurring_sub_desc   
0                              NaN                               NaN  \
1                              NaN                               NaN   
2                              NaN                               NaN   
3                              NaN                               NaN   
4                              NaN                               NaN   
...                            ...                               ...   
79496                          NaN                               NaN   
79497                          NaN                               NaN   
79498                          NaN                               NaN   
79499                          NaN                               NaN   
79500                          NaN                               NaN   

      alternate_appid  
0                 NaN  
1                 NaN  
2                 NaN  
3                 NaN  
4                 NaN  
...               ...  
79496             NaN  
79497             NaN  
79498             NaN  
79499             NaN  
79500             NaN  

[79501 rows x 64 columns]
```


#### Experiments in finding the total number of enums representing 'Categories' in the steam metadata set, as well as how to parse and extract them
```python
#Find every possible 'category' tag and keep only the ones that begin with 'id: [number]', which are the hard enums that Steam ACTUALLY uses
AllPossibleCategories = pd.DataFrame(app_metadata['categories'].apply(lambda x: re.findall(r'"id": \d+', x)).explode().unique(), columns=['category'])
AllPossibleCategories
```
```
    category
0    "id": 2
1   "id": 23
2   "id": 52
3   "id": 54
4   "id": 21
5   "id": 22
6   "id": 28
7   "id": 29
8   "id": 18
9   "id": 10
10   "id": 1
11  "id": 49
12  "id": 36
13   "id": 9
14  "id": 38
15  "id": 27
16  "id": 30
17  "id": 15
18  "id": 13
19  "id": 33
20  "id": 37
21  "id": 39
22  "id": 24
23  "id": 44
24  "id": 35
25  "id": 17
26  "id": 25
27   "id": 8
28  "id": 41
29  "id": 43
30  "id": 47
31  "id": 48
32       NaN
33  "id": 31
34  "id": 42
35  "id": 40
36  "id": 20
37  "id": 14
38  "id": 50
39  "id": 53
40  "id": 32
41  "id": 19
42  "id": 16
43  "id": 51
44   "id": 6
```

```python
#Find every possible 'category' tag and keep only the ones that begin with 'id: [number]', which are the hard enums that Steam ACTUALLY uses

#We also have to bring the "description" string along for the ride, otherwise we won't know what the enum actually means.

KnownCategoriesWithDescriptions = pd.DataFrame(app_metadata['categories'].apply(lambda x: re.findall(r'"id": \d+, "description": "[^"]+"', x)).explode().unique(), columns=['category'])
```

```python
# Split KnownCategoriesWithDescriptions into an 'enum' and 'description' column.
# We also need to extract the integer of the enum id in the 'enum' column.
KnownCategoriesWithDescriptions[['enum', 'description']] = KnownCategoriesWithDescriptions['category'].str.split(', "description": ', expand=True)
```

```python
#Remove 'id:' from the 'enum' column of KnownCategoriesWithDescriptions and convert it to an integer
KnownCategoriesWithDescriptions['enum'] = KnownCategoriesWithDescriptions['enum'].str.replace('"id": ', '').str.replace('"', '').astype(float).astype('Int64')
```

```python
KnownCategoriesWithDescriptions.dropna(inplace=True)
KnownCategoriesWithDescriptions.sort_values(by=['enum'], ascending=True)
```
```
                                              category  enum   
10              "id": 1, "description": "Multi-player"     1  \
197           "id": 1, "description": "Multigiocatore"     1   
79    "id": 1, "description": "Для нескольких игроков"     1   
212              "id": 1, "description": "Multiplayer"     1   
112             "id": 1, "description": "Wieloosobowa"     1   
..                                                 ...   ...   
44   "id": 50, "description": "Additional High-Qual...    50   
66           "id": 51, "description": "Steam Workshop"    51   
2    "id": 52, "description": "Tracked Controller S...    52   
45             "id": 53, "description": "VR Supported"    53   
3                   "id": 54, "description": "VR Only"    54   

                         description  
10                    "Multi-player"  
197                 "Multigiocatore"  
79          "Для нескольких игроков"  
212                    "Multiplayer"  
112                   "Wieloosobowa"  
..                               ...  
44   "Additional High-Quality Audio"  
66                  "Steam Workshop"  
2       "Tracked Controller Support"  
45                    "VR Supported"  
3                          "VR Only"  

[218 rows x 3 columns]
```

```python
KnownCategoriesWithDescriptions.sort_values(by=['enum'], ascending=True)['enum'].unique()
```
```
<IntegerArray>
[ 1,  2,  6,  8,  9, 10, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25,
 27, 28, 29, 30, 31, 32, 33, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 47, 48,
 49, 50, 51, 52, 53, 54]
Length: 44, dtype: Int64
```

```python
KnownCategoriesWithDescriptions[KnownCategoriesWithDescriptions['enum'] == 49]
```
```
                                      category  enum       description
11              "id": 49, "description": "PvP"    49             "PvP"
80   "id": 49, "description": "Против игроков"    49  "Против игроков"
128             "id": 49, "description": "JcJ"    49             "JcJ"
156             "id": 49, "description": "JxJ"    49             "JxJ"
```

```python
# Find all category int values for a given game. Expects a row of the app_metadata dataframe.
def find_categories(row):
    categories = re.findall(r'"id": \d+', row['categories'])
    categories = [int(re.findall(r'\d+', category)[0]) for category in categories]
    return categories

#Example usage
find_categories(app_metadata.iloc[0])
```
```
[2, 23]
```

```python
# Exact copy-paste matches for category ID to category name
# 1: "Multi-player"
# 2: "Single-player"
# 3.
# 4.
# 5.
# 6. "Mods (require HL2)"
# 7.
# 8. "Valve Anti-Cheat enabled"
# 9. "Co-op"
# 10. "Game demo"
# 11.
# 12. 
# 13. "Captions available"
# 14. "Commentary available"
# 15. "Stats"
# 16. "Includes Source SDK"
# 17. "Includes level editor"
# 18. "Partial Controller Support"
# 19. "Mods"
# 20. "MMO"
# 21. "Downloadable Content"
# 22. "Steam Achievements"
# 23. "Steam Cloud"
# 24. "Shared/Split Screen"
# 25. "Steam Leaderboards"
# 27. "Cross-Platform Multiplayer"
# 28. "Full controller support"
# 29. "Steam Trading Cards"
# 30. "Steam Workshop"
# 31. "VR Support"
# 32. "Steam Turn Notifications"
# 33. "Native Steam Controller Support"
# 35. "In-App Purchases"
# 36. "Online PvP"
# 37. "Shared/Split Screen PvP"
# 38. "Online Co-op"
# 39. "Shared/Split Screen Co-op"
# 40. "SteamVR Collectibles"
# 41. "Remote Play on Phone"
# 42. "Remote Play on Tablet"
# 43. "Remote Play on TV"
# 44. "Remote Play Together"
# 45.
# 46.
# 47. "LAN PvP"
# 48. "LAN Co-op"
# 49. "PvP"
# 50. "Additional High-Quality Audio"
# 51. "Steam Workshop"
# 52. "Tracked Controller Support"
# 53. "VR Supported"
# 54. "VR Only"
```

#### Experiments in finding the total number of languages represented in all Steam reviews

```python
##### DANGER ZONE #####
##### TAKES A LONG TIME TO RUN #####
##### ONLY USED TO TAKE A CENSUS OF LANGUAGES #####

language_strings_found = []

# Among all reviews in the raw_reviews_folder path, take a census and find all unique languages that reviews have been found in.
# This is to help us determine which languages we should support in our database.
# The raw_reviews_folder contains both csv and non-csv files. We only want to sample from csvs.
print(f"Finding all raw review files in {raw_reviews_folder}...")
csv_files = [file for file in os.listdir(raw_reviews_folder) if file.endswith('.csv')]
print(f"Found {len(csv_files)} csv files.")

cur_index = -1
for file in csv_files[:1000]:
    cur_index += 1
    if cur_index % 1000 == 0:
        language_strings_found = list(set(language_strings_found)) # Remove duplicates
    try:
        reviews = pd.read_csv(os.path.join(raw_reviews_folder, file))
        language_strings_found.extend(reviews['language'].unique())
    except Exception as e:
        print(f"Error reading file {file}: {e}")
        continue


language_strings_found = list(set(language_strings_found)) # Remove duplicates
language_strings_found

```
```
Finding all raw review files in ../../DB-Generation/Downloaded_Reviews/...
Found 82399 csv files.
```
```
['latam',
 'vietnamese',
 'dutch',
 'norwegian',
 'romanian',
 'danish',
 'russian',
 'japanese',
 'portuguese',
 'ukrainian',
 'french',
 'italian',
 'bulgarian',
 'polish',
 'turkish',
 'brazilian',
 'german',
 'hungarian',
 'schinese',
 'czech',
 'swedish',
 'koreana',
 'tchinese',
 'greek',
 'spanish',
 'thai',
 'finnish',
 'english']
```

#### Filtering out a merciless onslaught of garbage data

```python
#Creation and filtering of the app metadata frame. 
#Here we do all the filtering that can reasonably be done from exclusively the metadata.
#Many bad actors still push games through, so we have to perform additional filtering from the game titles. 
orig_app_count = len(app_metadata)
filtered_metadata = app_metadata.copy()
print("Original metadata length: ", len(filtered_metadata))
filtered_metadata = filtered_metadata.drop_duplicates(subset=['steam_appid'])
print("length of filtered_metadata after duplicate pass: ", len(filtered_metadata))
filtered_metadata = filtered_metadata[filtered_metadata['type'] == 'game']
print("length of filtered_metadata after type pass: ", len(filtered_metadata))
#Remove 18+ games. Remove any '+' from the age rating and convert to float to check it.
filtered_metadata = filtered_metadata[filtered_metadata['required_age'].apply(lambda x: re.sub(r'\+', '', x)).astype(float) < 18]
print("length of filtered_metadata after age pass: ", len(filtered_metadata))
#Remove any games which have not released
filtered_metadata = filtered_metadata[filtered_metadata['release_date.coming_soon'] == 'False']
print("length of filtered_metadata after release pass: ", len(filtered_metadata))
#Remove any games with empty, NA, or NaN short descriptions
filtered_metadata = filtered_metadata[filtered_metadata['short_description'].notna()]
print("length of filtered_metadata after short description pass: ", len(filtered_metadata))
#Remove any games with empty, NA, or NaN detailed descriptions
filtered_metadata = filtered_metadata[filtered_metadata['detailed_description'].notna()]
print("length of filtered_metadata after detailed description pass: ", len(filtered_metadata))

# Skipping naughty words filter pass so that we find all games that have reviews, even if they have naughty words in their descriptions.
filter_words = ['hentai']
filtered_metadata = filtered_metadata[~filtered_metadata['name'].str.contains(''.join(filter_words), case=False)]
filtered_metadata = filtered_metadata[~filtered_metadata['short_description'].str.contains(''.join(filter_words), case=False)]
filtered_metadata = filtered_metadata[~filtered_metadata['detailed_description'].str.contains(''.join(filter_words), case=False)]
print("length of filtered_metadata after filter words pass: ", len(filtered_metadata))

appids_with_any_reviews = [f.replace('.csv', '') for f in os.listdir(raw_reviews_folder)]
filtered_metadata = filtered_metadata[(filtered_metadata['steam_appid'].isin(appids_with_any_reviews))]
print("length of filtered_metadata after reviews pass: ", len(filtered_metadata))

print("Final length of filtered_metadata: ", len(filtered_metadata))
print("Number of games removed: ", orig_app_count - len(filtered_metadata))
```
```
Original metadata length:  79501
length of filtered_metadata after duplicate pass:  79444
length of filtered_metadata after type pass:  47062
length of filtered_metadata after age pass:  46872
length of filtered_metadata after release pass:  38447
length of filtered_metadata after short description pass:  37079
length of filtered_metadata after detailed description pass:  37078
length of filtered_metadata after filter words pass:  36764
length of filtered_metadata after reviews pass:  30457
Final length of filtered_metadata:  30457
Number of games removed:  49044
```

#### Checkpoint 1 - Methods for imputing new information about the dataset

#### Preprocess Text

```python
#Method for cleaning up review and store page texts
def preprocess_text(text):
    if not isinstance(text, str):
        return ''  # Return an empty string if the input is not a string
    text = re.sub(r'<[^<]+?>', ' ', text)  # Remove formatting tags
    text = re.sub(r'\[.*?\]', " ", text)   # Remove block tags
    text = re.sub(r'http\S+', " ", text)   # Remove web links
    #Commented out one processing step which is prone to work poorly with multilingual inputs.
    #text = re.sub(r'^[^a-zA-Z]+', " ", text)  # Remove non-alphabetical characters at the beginning of a string (for dashed lists)
    text = re.sub(r'quot;', "", text)  # Remove front quotes
    text = re.sub(r'&quot', "", text)  # Remove back quotes
    text = re.sub(r'\n', " ", text)    # Remove newlines
    text = re.sub(r'\-\-+', ' ', text)  # Remove extra dashes (2+ dashes become a single space, single dashes are kept in)
    text = re.sub(r'\.\.+', '. ', text)  # Remove extra periods (2+ periods become a period with a single space, single periods are kept in)
    text = re.sub(r'\\r', ' ', text)    # Remove \r replies
    text = re.sub(r'(((.)\3+)\3+)\3+', r'\3', text)  # Remove extra characters (4+ of the same letter become a single letter, single letters are kept in)
    text = re.sub(r'\\', ' ', text)     # Remove any remaining backslashes
    text = re.sub(r'\s+', ' ', text)    # Remove extra whitespace, tab delimiters, etc.
    text = re.sub(r' +', ' ', text)     # Remove extra spaces
    return text
```

#### Words Detect

```python
import re

common = r'(\d+|[a-zA-Z\u00C0-\u00FF\u0100-\u017F\u0180-\u024F\u0250-\u02AF\u1E00-\u1EFF\u0400-\u04FF\u0500-\u052F\u0D00-\u0D7F]+)'
cjk = r'[\u2E80-\u2EFF\u2F00-\u2FDF\u3000-\u303F\u31C0-\u31EF\u3200-\u32FF\u3300-\u33FF\u3400-\u4DBF\u4E00-\u9FFF\uF900-\uFAFF]'
jp = r'[\u3040-\u309F\u30A0-\u30FF\u31F0-\u31FF\u3190-\u319F]'
kr = r'[\u1100-\u11FF\u3130-\u318F\uA960-\uA97F\uAC00-\uD7FF]'
reg = re.compile(
    cjk + '|' + jp + '|' + kr,
    re.UNICODE
)

DEFAULT_PUNCTUATION = [
    ',', '，', '.', '。', ':', '：', ';', '；', '[', ']', '【', ']', '】', '{', '｛', '}', '｝',
    '(', '（', ')', '）', '<', '《', '>', '》', '$', '￥', '!', '！', '?', '？', '~', '～',
    "'", '’', '"', '“', '”',
    '*', '/', '\\', '&', '%', '@', '#', '^', '、', '、', '、', '、'
]

def words_detect(text, config=None):
    if config is None:
        config = {}
    if not text:
        return {'words': [], 'count': 0, 'unique_count': 0}
    words = str(text)
    if words.strip() == '':
        return {'words': [], 'count': 0, 'unique_count': 0}
    punctuation_replacer = ' ' if config.get('punctuationAsBreaker') else ''
    default_punctuations = [] if config.get('disableDefaultPunctuation') else DEFAULT_PUNCTUATION
    customized_punctuations = config.get('punctuation', [])
    combined_punctuations = default_punctuations + customized_punctuations
    for punctuation in combined_punctuations:
        words = words.replace(punctuation, punctuation_replacer)
    words = re.sub(r'\s+', ' ', words).strip()  # strip leading and trailing spaces
    words = words.split(' ')
    words = [word for word in words if word.strip()]
    
    detected_words = []
    for word in words:
        if reg.match(word[0]):  # if the first character of the word matches the regex, treat it as a CJK word
            detected_words.extend(list(word))  # split the CJK word into characters
        else:  # if the first character doesn't match the regex, treat it as an English word
            detected_words.append(word)
    
    return {
        'words': detected_words,
        'count': len(detected_words),
        'unique_count': len(set(detected_words))
    }

chinese_review = "这是一个中文句子。"
japanese_review = "これは日本語の文です。"
korean_review = "이것은 한국어 문장입니다."

print(words_detect(chinese_review, config={'punctuationAsBreaker': True}))
print(words_detect(japanese_review, config={'punctuationAsBreaker': True}))
print(words_detect(korean_review, config={'punctuationAsBreaker': True}))

fake_review = "its funny because i played the deleted scenes version first, then i forgot i mean't to play this one first, i don't really care but lol."
print(words_detect(fake_review, config={'punctuationAsBreaker': True}))
```
```
{'words': ['这', '是', '一', '个', '中', '文', '句', '子'], 'count': 8, 'unique_count': 8}
{'words': ['こ', 'れ', 'は', '日', '本', '語', 'の', '文', 'で', 'す'], 'count': 10, 'unique_count': 10}
{'words': ['이', '것', '은', '한', '국', '어', '문', '장', '입', '니', '다'], 'count': 11, 'unique_count': 11}
{'words': ['its', 'funny', 'because', 'i', 'played', 'the', 'deleted', 'scenes', 'version', 'first', 'then', 'i', 'forgot', 'i', 'mean', 't', 'to', 'play', 'this', 'one', 'first', 'i', 'don', 't', 'really', 'care', 'but', 'lol'], 'count': 28, 'unique_count': 23}
```

#### Remove Common Fake Reviews

```python
def remove_common_fake_reviews(reviews):
    common_fake_reviews = [
        'this game saved my life',
        'my name is walter hartwell white'
        '☑',
        '✓'
    ]

    # For each 'processed_review' text, check if any of its substrings are in the 'common_fake_reviews' list.
    # If so, mark the review for removal.
    mask = reviews['processed_review'].apply(lambda review: any(fake_review in review.lower() for fake_review in common_fake_reviews))

    # Use the mask to filter out the rows that contain common fake reviews.
    # Use .copy() to explicitly create a new DataFrame.
    return reviews[~mask].copy()
```

```python
test_reviews = pd.read_csv(os.path.join(raw_reviews_folder, '307110.csv'))
test_reviews
```
```
      recommendationid language   
0            138508251   polish  \
1            138174091  english   
2            138169488  english   
3            138143000   german   
4            138072738  english   
...                ...      ...   
4054          29784829  russian   
4055          29784739  russian   
4056          29783994  english   
4057          29782735  english   
4058          29781518  english   

                                                 review  timestamp_created   
0                                       dont play alone         1684441373  \
1     Its a great simplistic 2D game with epic boss ...         1683880193   
2     Not a game for everyone, but if you can get pa...         1683867612   
3                                           sehr schuun         1683821276   
4                   should be more popular its great :)         1683681592   
...                                                 ...                ...   
4054  Я понимаю, что рано писать обзор, толком не по...         1486571699   
4055  этакий таймкиллер -минмум на троих\nИз плюсов:...         1486571411   
4056  *Early-access review!* We Need To Go Deeper is...         1486568759   
4057                                               Cool         1486564679   
4058           10/10 i could drive this thing by myself         1486560613   

      timestamp_updated  voted_up  votes_up  votes_funny  weighted_vote_score   
0            1684441373      True         0            0             0.000000  \
1            1683880193      True         0            0             0.000000   
2            1683867612      True         0            0             0.476190   
3            1683821276      True         0            0             0.000000   
4            1683681592      True         0            0             0.000000   
...                 ...       ...       ...          ...                  ...   
4054         1486669881      True        12            1             0.609020   
4055         1486571533      True         3            1             0.485045   
4056         1486647671      True        14            0             0.560389   
4057         1486564679      True         1            0             0.261606   
4058         1486560613      True         7           16             0.314048   

      comment_count  ...  written_during_early_access  hidden_in_steam_china   
0                 0  ...                        False                   True  \
1                 0  ...                        False                   True   
2                 0  ...                        False                   True   
3                 0  ...                        False                   True   
4                 0  ...                        False                   True   
...             ...  ...                          ...                    ...   
4054              0  ...                         True                  False   
4055              0  ...                         True                  False   
4056              8  ...                         True                  False   
4057              0  ...                         True                  False   
4058              4  ...                         True                  False   

      steam_china_location     author.steamid  author.num_games_owned   
0                      NaN  76561199013327143                       0  \
1                      NaN  76561199121928163                       0   
2                      NaN  76561198855406533                       0   
3                      NaN  76561198930003688                      75   
4                      NaN  76561198798534120                       0   
...                    ...                ...                     ...   
4054                   NaN  76561197993039039                       0   
4055                   NaN  76561197993263781                       0   
4056                   NaN  76561198012696724                     220   
4057                   NaN  76561198055703811                     553   
4058                   NaN  76561198134196365                     312   

      author.num_reviews  author.playtime_forever   
0                      6                     3024  \
1                     19                     1834   
2                      8                     1363   
3                      2                     1250   
4                      1                      332   
...                  ...                      ...   
4054                 229                      604   
4055                   1                      265   
4056                  14                       38   
4057                 134                     1296   
4058                  31                     2376   

      author.playtime_last_two_weeks  author.playtime_at_review   
0                                207                     3024.0  \
1                               1834                      502.0   
2                                500                     1358.0   
3                                129                     1120.0   
4                                257                      262.0   
...                              ...                        ...   
4054                               0                      604.0   
4055                               0                      213.0   
4056                               0                       38.0   
4057                               0                       78.0   
4058                               0                       53.0   

      author.last_played  
0             1684441217  
1             1684434043  
2             1683867744  
3             1684167756  
4             1683779278  
...                  ...  
4054          1486654501  
4055          1488984373  
4056          1486653262  
4057          1615659084  
4058          1627853323  

[4059 rows x 22 columns]
```

```python
test_reviews['steam_purchase'].value_counts()
```
```
steam_purchase
True    4059
Name: count, dtype: int64
```

```python
test_reviews['received_for_free'].value_counts()
```
```
received_for_free
False    3989
True       70
Name: count, dtype: int64
```

#### Process Reviews

```python
def process_reviews(appid, raw_reviews_folder, min_reviews=10, min_word_count=20, min_unique_word_count = 20, upper_word_count_clip = 2000, upper_playtime_clip = 1000):
    # Construct the file path
    file_path = os.path.join(raw_reviews_folder, str(appid) + '.csv')

    # Check if the file exists
    if not os.path.isfile(file_path):
        return None

    # Load the reviews
    reviews = pd.read_csv(file_path, quoting=csv.QUOTE_ALL)

    # Check if the DataFrame is empty or has less than num_reviews
    if reviews.empty or len(reviews) < min_reviews:
        return None

    # Process the reviews
    reviews.loc[:, 'processed_review'] = reviews['review'].apply(lambda x: preprocess_text(x))
    reviews = remove_common_fake_reviews(reviews)

    # Check if the DataFrame is empty or has less than num_reviews after removing common fake reviews
    if reviews.empty or len(reviews) < min_reviews:
        return None

    reviews.loc[:, 'word_count'] = reviews['processed_review'].apply(lambda x: words_detect(x, config={'punctuationAsBreaker': True})['count'])
    reviews.loc[:, 'unique_word_count'] = reviews['processed_review'].apply(lambda x: words_detect(x, config={'punctuationAsBreaker': True})['unique_count'])

    # Drop the reviews that have less than the min word count and unique word count
    reviews = reviews[reviews['word_count'] >= min_word_count]
    reviews = reviews[reviews['unique_word_count'] >= min_unique_word_count]

    if reviews.empty or len(reviews) < min_reviews:
        return None

    # Resonance score is calculated as follows:
    # The geometric mean of word count, unique word count, and playtime forever (up to 1000 hours) are taken.
    # This mean of words and playtime is then multiplied by the score Steam assigned that review based on helpful/unhelpful/funny votes. 
    # We have to add 1 to their score because their scale is -1 to 1 with most values betwen 0 and 0.5.
    used_word_counts = reviews['word_count'].astype(int)
    used_word_counts = used_word_counts.clip(lower = 1, upper = upper_word_count_clip)
    used_unique_word_counts = reviews['unique_word_count'].astype(int)
    used_unique_word_counts = used_unique_word_counts.clip(lower = 1, upper = upper_word_count_clip)
    used_playtime = reviews['author.playtime_forever'].astype(float)
    used_playtime = used_playtime.clip(lower = 1, upper = upper_playtime_clip)
    resonance_score = ((used_word_counts * used_playtime * used_playtime) ** (1/3)) * (reviews['weighted_vote_score'] + 1)

    reviews.loc[:, 'resonance_score'] = resonance_score
    
    #Convert the 'timestamp_created' column to a datetime object
    reviews.loc[:, 'datetime_timestamp_created'] = reviews.loc[:, 'timestamp_created'].apply(lambda x: datetime.fromtimestamp(x))
    reviews.loc[:, 'steam_release_year'] = reviews['datetime_timestamp_created'].min().year

    # Drop useless columns and add an appid column.
    reviews = reviews.drop(columns=[
        'review',                       #We now have our own processed version of this column
        'hidden_in_steam_china', 
        'steam_china_location',
        'timestamp_created',            #We now have our own formatted version of this column
        'timestamp_updated',
        'votes_up',
        'votes_funny',
        'comment_count',
        'steam_purchase',               #We already filtered for this during the collection step
        'received_for_free',            #We already filtered for this during the collection step
        'written_during_early_access', 
        'author.steamid', 
        'author.playtime_last_two_weeks', 
        'author.last_played'
        ])
    reviews.loc[:, 'appid'] = int(appid)


    return reviews
```

```python
process_reviews(307110, raw_reviews_folder).dtypes
```
```
recommendationid                       int64
language                              object
review                                object
timestamp_created                      int64
voted_up                                bool
votes_up                               int64
votes_funny                            int64
weighted_vote_score                  float64
comment_count                          int64
steam_purchase                          bool
received_for_free                       bool
author.num_games_owned                 int64
author.num_reviews                     int64
author.playtime_forever                int64
author.playtime_at_review            float64
appid                                  int64
processed_review                      object
word_count                             int64
unique_word_count                      int64
resonance_score                      float64
datetime_timestamp_created    datetime64[ns]
dtype: object
```



---

#### Analysis - Data Pipeline is Agnostic to Language

##### Word Count Filtering Does Not Alter Language Distribution in Reviews that are Kept

```python
test_bulk_multilingual_reviews = pd.read_csv(os.path.join(raw_reviews_folder, '10.csv'), quoting=csv.QUOTE_ALL)
```

```python
test_processed_bulk_reviews = process_reviews(10, raw_reviews_folder)
```

```python
len(test_processed_bulk_reviews['language'].unique())
```
```
28
```

```python
#Plot the number of reviews BEFORE preprocessing by language.
test_bulk_multilingual_reviews.groupby('language').size().plot(kind='bar')
```
![[Pasted image 20240929212411.png]]

```python
#Plot the number of reviews by language AFTER preprocessing.
test_processed_bulk_reviews.groupby('language').size().plot(kind='bar')
```
![[Pasted image 20240929212436.png]]

```python
#Find the average resonance score by language.
test_processed_bulk_reviews.groupby('language')['resonance_score'].mean().plot(kind='bar')
```
![[Pasted image 20240929212458.png]]

>[!Info] Notice above:
>In the processed reviews Thai stands above the others. This appears to be due to a low sample size problem, where Thai reviews are underrepresented in the dataset. 

##### Picking out the top 100-1000 reviews does not appear to impact distribution of languages or distribution of resonance

```python
test_processed_bulk_reviews.sort_values(by='resonance_score', ascending=False).head(500).groupby('language').size().plot(kind='bar')
```
![[Pasted image 20240929212706.png]]

```python
test_processed_bulk_reviews.sort_values(by='resonance_score', ascending=False).head(200).groupby('language')['resonance_score'].mean().plot(kind='bar')
```
![[Pasted image 20240929212724.png]]

```python
test_processed_bulk_reviews.sort_values(by='resonance_score', ascending=False)
```
```
        recommendationid  language  voted_up  weighted_vote_score   
68371           55550684  schinese      True             0.519731  \
62816           60079131  schinese      True             0.880664   
99290           31861094   russian      True             0.635531   
35942           89772168   russian     False             0.578655   
47286           75402392   english      True             0.624590   
...                  ...       ...       ...                  ...   
134632            486254  japanese      True             0.523810   
134989           4025997   english      True             0.000000   
135247            634250    french      True             0.000000   
133530            871968   english      True             0.000000   
134993            741038   english      True             0.000000   

        author.num_games_owned  author.num_reviews  author.playtime_forever   
68371                        0                   3                   159412  \
62816                        0                  41                     2576   
99290                        0                   1                    22501   
35942                       34                   7                     2971   
47286                        0                   2                   328973   
...                        ...                 ...                      ...   
134632                       0                   6                        0   
134989                       0                  67                        0   
135247                    2770                 397                        0   
133530                       0                  34                        1   
134993                       0                 369                        0   

        author.playtime_at_review   
68371                      7907.0  \
62816                      1793.0   
99290                      7367.0   
35942                      2006.0   
47286                    238825.0   
...                           ...   
134632                        NaN   
134989                        NaN   
135247                        NaN   
133530                        NaN   
134993                        NaN   

                                         processed_review  word_count   
68371   （一）个人总结：我玩的过程中有乐趣、我体会到的“游戏内总体氛围”不理想、我觉得值得玩这个游戏...        3801  \
62816   如果你从小到大都没有玩过CS，甚至没玩过几款FPS，那么我不建议你入手这款CS。 原因如下：...         952   
99290   Начнем с того что это за игра Counter – Strike...        1236   
35942   Эххх игра хорошая , в любом случае в неё играл...        1159   
47286   This game is so old, there are people born lat...        1051   
...                                                   ...         ...   
134632                   無料の頃の、あの楽しさは、もう戻って来ない・・・のかもしれません          30   
134989  There are thousands of better multi-player sho...          56   
135247  + Communauté gigantesque et vivante même après...          35   
133530  Good old classic Counter-strike, sadly. a bit ...          24   
134993  I think i watched a high school friend play th...          22   

        unique_word_count  resonance_score datetime_timestamp_created  appid  
68371                 646      1914.741509        2019-10-11 08:10:28     10  
62816                 379      1850.078535        2019-12-18 13:00:24     10  
99290                 734      1755.219743        2017-05-20 09:52:18     10  
35942                 588      1658.243714        2021-04-05 10:31:58     10  
47286                 489      1651.751066        2020-09-03 22:01:56     10  
...                   ...              ...                        ...    ...  
134632                 23         4.734831        2012-02-21 18:32:05     10  
134989                 53         3.825862        2011-08-22 12:49:00     10  
135247                 32         3.271066        2010-12-03 13:04:09     10  
133530                 22         2.884499        2013-05-31 22:23:14     10  
134993                 22         2.802039        2011-08-18 20:20:56     10  

[13377 rows x 14 columns]
```

#### Experiments in finding the precise resonance cutoff we want to use

```python
##### DANGER ZONE #####
##### TAKES A LONG TIME TO RUN #####

# Randomly sample and process N appids worth of reviews,
# formatting the output dataframe with more efficient dtypes.
# Include the appid as a column.

print(f"Finding all raw review files in {raw_reviews_folder}...")
csv_files = [file for file in os.listdir(raw_reviews_folder) if file.endswith('.csv')]
print(f"Found {len(csv_files)} csv files.")

sampled_reviews = process_reviews(307110, raw_reviews_folder) #Start with Deeper and then use pd.concat to add new reviews with correct dtypes.
cur_index = -1
random_sample_size = 1000
for file in random.sample(csv_files, random_sample_size):
    cur_index += 1
    if cur_index % 100 == 0:
        print(f"Processing file {cur_index} of {random_sample_size}...")
        print(f"Current file: {file}")
    try:
        appid = int(file.split('.')[0])
        reviews = process_reviews(appid, raw_reviews_folder)
        if reviews is None:
            continue
        sampled_reviews = pd.concat([sampled_reviews, reviews], ignore_index=True)
    except Exception as e:
        print(f"Error reading file {file}: {e}")
        continue

sampled_reviews.drop_duplicates(subset=['recommendationid'], inplace=True)
sampled_reviews
```
```
Finding all raw review files in ../../DB-Generation/Downloaded_Reviews/...
Found 83848 csv files.
Processing file 0 of 1000...
Current file: 1916750.csv
C:\Users\User\AppData\Local\Temp\ipykernel_25540\3402533940.py:10: DtypeWarning: Columns (14) have mixed types. Specify dtype option on import or set low_memory=False.
  reviews = pd.read_csv(file_path, quoting=csv.QUOTE_ALL)
Processing file 100 of 1000...
Current file: 1653910.csv
Processing file 200 of 1000...
Current file: 510530.csv
Processing file 300 of 1000...
Current file: 1157732.csv
Processing file 400 of 1000...
Current file: 1032790.csv
Processing file 500 of 1000...
Current file: 1879750.csv
Processing file 600 of 1000...
Current file: 1894770.csv
Processing file 700 of 1000...
Current file: 1222170.csv
Processing file 800 of 1000...
Current file: 451180.csv
Processing file 900 of 1000...
Current file: 701280.csv
```
```
        recommendationid language  voted_up  weighted_vote_score   
0              138169488  english      True             0.476190  \
1              137998280  english      True             0.000000   
2              137324445  english     False             0.551387   
3              136922264   french      True             0.000000   
4              136540892   german      True             0.000000   
...                  ...      ...       ...                  ...   
197442          29432416  english     False             0.484038   
197443          29415774  english      True             0.000000   
197444          29413485  english     False             0.562283   
197445          29407123  english     False             0.523810   
197446          28427209  english      True             0.440910   

        author.num_games_owned  author.num_reviews  author.playtime_forever   
0                            0                   8                     1363  \
1                            0                  16                      571   
2                          420                  99                      145   
3                          117                  16                     3689   
4                            0                   2                     2233   
...                        ...                 ...                      ...   
197442                       0                 110                      406   
197443                       0                   8                     1020   
197444                     371                   9                      282   
197445                       0                   1                     9916   
197446                     225                  96                     5942   

        author.playtime_at_review   
0                          1358.0  \
1                           571.0   
2                           145.0   
3                          3361.0   
4                          2233.0   
...                           ...   
197442                      406.0   
197443                     1020.0   
197444                      200.0   
197445                     3342.0   
197446                     5137.0   

                                         processed_review  word_count   
0       Not a game for everyone, but if you can get pa...          37  \
1       its ok if you have a bunch of friends to play ...          25   
2       You die, you reopen the lobby you re-set the r...          61   
3       Jeu exceptionnel, seul bémol c'est pour trouve...          58   
4       Dieses Spiel ist nur geeignet, wenn man ein di...          36   
...                                                   ...         ...   
197442  Game is stealing money. Don't bother with it. ...          35   
197443  Ok. I like the game, and recommend you at leas...         133   
197444  The game developer(s) need to fix the issues w...          48   
197445  on several times i have log out only to come b...          37   
197446  I have owned a steam account since the beginni...         259   

        unique_word_count  resonance_score datetime_timestamp_created   
0                      32       491.899417        2023-05-12 01:00:12  \
1                      22       201.250704        2023-05-08 09:20:28   
2                      50       168.555139        2023-04-26 10:25:07   
3                      48       387.087664        2023-04-18 15:44:56   
4                      31       330.192725        2023-04-11 15:54:02   
...                   ...              ...                        ...   
197442                 30       266.165471        2017-01-22 17:25:31   
197443                 92       510.446872        2017-01-22 02:35:51   
197444                 35       244.159083        2017-01-21 23:19:53   
197445                 34       507.767149        2017-01-21 15:55:11   
197446                151       918.480581        2016-12-17 10:22:18   

        steam_release_year   appid  
0                     2017  307110  
1                     2017  307110  
2                     2017  307110  
3                     2017  307110  
4                     2017  307110  
...                    ...     ...  
197442                2016  251450  
197443                2016  251450  
197444                2016  251450  
197445                2016  251450  
197446                2016  251450  

[197447 rows x 15 columns]
```

```python
# Plot a histogram of resonance score measured across all sampled game reviews.
sampled_reviews['resonance_score'].hist(bins=100)
```
![[Pasted image 20240929212924.png]]

```python
# Zoom in on that plot to only look at the activity between 20 and 1000.
sampled_reviews['resonance_score'].hist(bins=100, range=(20,1000))
```
![[Pasted image 20240929212944.png]]

```python
len(sampled_reviews[sampled_reviews['resonance_score'] >= 250]) / len(sampled_reviews)
```
```
0.6537239979023518
```

##### Sub-Experiment: Looking for biases introduced by time since release

```python
# Use the 'datetime_timestamp_created' column to plot the cumulative number of reviews over time.

sampled_reviews['datetime_timestamp_created'].hist(bins=100)
```
![[Pasted image 20240929213024.png]]

```python
import matplotlib.pyplot as plt

# Step 1: Group by 'appid' and 'steam_release_year', then count the number of rows for each group
reviews_per_game = sampled_reviews.groupby(['appid', 'steam_release_year']).size().reset_index(name='review_count')

# Step 2: Group by 'steam_release_year' to calculate the mean of these counts
avg_reviews_per_year = reviews_per_game.groupby('steam_release_year')['review_count'].mean()

# Plotting
avg_reviews_per_year.plot(kind='line')
plt.title('Average Number of Reviews per Game by Release Year')
plt.xlabel('Release Year')
plt.ylabel('Average Number of Reviews')
plt.show()
```
![[Pasted image 20240929213203.png]]

```python
import seaborn as sns

# Use Seaborn to create a box plot
sns.boxplot(x='steam_release_year', y='review_count', data=reviews_per_game)
plt.yscale('log')
plt.title('Distribution of Number of Reviews per Game by Release Year')
plt.xlabel('Release Year')
plt.ylabel('Number of Reviews')
plt.xticks(rotation=90)  # Rotate x-axis labels if they overlap
plt.show()

```
![[Pasted image 20240929213225.png]]

```python
# Violin plot with log scale
sns.violinplot(x='steam_release_year', y='review_count', data=reviews_per_game)
plt.yscale('log')
plt.title('Distribution of Number of Reviews per Game by Release Year (Log Scale)')
plt.xlabel('Release Year')
plt.ylabel('Number of Reviews (Log Scale)')
plt.xticks(rotation=90)
plt.show()
```
![[Pasted image 20240929213259.png]]

```python
avg_reviews_per_year
```
```
steam_release_year
2010    1577.454545
2011     925.750000
2012      55.750000
2013     166.666667
2014     442.117647
2015     123.357143
2016    3641.702128
2017     324.471698
2018    1422.431818
2019     243.333333
2020     882.644444
2021     210.163636
2022      72.520833
2023     106.227273
Name: review_count, dtype: float64
```



---

#### Utility Methods


##### Shorten Text to Max Tokens

```python
# Get the encoding for a specific model
encoding = tiktoken.encoding_for_model("gpt-3.5-turbo")

def count_tokens(text):
    # Encode the text into tokens
    tokens = encoding.encode(text)
    return len(tokens)

def shorten_text_to_max_tokens(text, token_limit=8000):
    # Encode the text into tokens
    tokens = encoding.encode(text)
    # Shorten the text to the maximum number of tokens
    return encoding.decode(tokens[:token_limit])
```

##### Embed

```python
# Extract embedding from text using OpenAI
def embed(text):
    return openai.Embedding.create(
        input=text, 
        engine=OPENAI_ENGINE)["data"][0]["embedding"]
```

##### Get Bulk Review Stats

```python
def get_bulk_review_stats(processed_reviews):

    # Simple properties we collect.
    positivity_rating = len(processed_reviews[processed_reviews['voted_up'] == True]) / len(processed_reviews)

    # The mathier bulk properties we collect.
    geometric_mean_word_count = gmean(processed_reviews['word_count'].clip(lower=1))
    geometric_mean_unique_word_count = gmean(processed_reviews['unique_word_count'].clip(lower=1))
    geometric_mean_resonance_score = gmean(processed_reviews['resonance_score'].clip(lower=1))
    geometric_mean_hours_played = gmean(processed_reviews['author.playtime_forever'].clip(lower=1))
    geometric_mean_num_games_owned = gmean(processed_reviews['author.num_games_owned'].clip(lower=1))
    geometric_mean_author_num_reviews = gmean(processed_reviews['author.num_reviews'].clip(lower=1))
    
    #Convert Linux timestamps for review creation into datetime objects, 
    #then find the date of the oldest review
    first_review_date = processed_reviews.loc[:, 'datetime_timestamp_created'].min().date()
    last_review_date = processed_reviews.loc[:, 'datetime_timestamp_created'].max().date()
    inferred_release_year = first_review_date.year

    return {
        'positivity_rating': positivity_rating,
        'geometric_mean_word_count': geometric_mean_word_count,
        'geometric_mean_unique_word_count': geometric_mean_unique_word_count,
        'geometric_mean_resonance_score': geometric_mean_resonance_score,
        'geometric_mean_hours_played': geometric_mean_hours_played,
        'geometric_mean_num_games_owned': geometric_mean_num_games_owned,
        'geometric_mean_author_num_reviews': geometric_mean_author_num_reviews,
        'first_review_date': first_review_date.strftime('%Y-%m-%d'),
        'last_review_date': last_review_date.strftime('%Y-%m-%d'),
        'inferred_release_year': int(inferred_release_year)
    }
```

##### Find Categories

```python
# Find all category int values for a given game. Expects a row of the app_metadata dataframe.
def find_categories(row):
    categories = re.findall(r'"id": \d+', row['categories'])
    categories = [int(re.findall(r'\d+', category)[0]) for category in categories]
    return categories

#Example usage
find_categories(app_metadata.iloc[0])
```
```
[2, 23]
```

```python
known_category_enums
```
```
    Category ID                    Category Name
0             1                     Multi-player
1             2                    Single-player
2             9                            Co-op
3            13               Captions available
4            18       Partial Controller Support
5            19                             Mods
6            20                              MMO
7            22               Steam Achievements
8            23                      Steam Cloud
9            24              Shared/Split Screen
10           27       Cross-Platform Multiplayer
11           28          Full controller support
12           30                   Steam Workshop
13           31                       VR Support
14           33  Native Steam Controller Support
15           35                 In-App Purchases
16           36                       Online PvP
17           37          Shared/Split Screen PvP
18           38                     Online Co-op
19           39        Shared/Split Screen Co-op
20           41             Remote Play on Phone
21           42            Remote Play on Tablet
22           43                Remote Play on TV
23           44             Remote Play Together
24           47                          LAN PvP
25           48                        LAN Co-op
26           49                              PvP
27           51                   Steam Workshop
28           52       Tracked Controller Support
29           53                     VR Supported
30           54                          VR Only
```

```python
# Define a function which extracts the 'category' tags from a given metadata row, 
# then returns a dictionary of English category names coupled to True or False values for each category available in the tag table.
def category_tags_to_bools(metadata_row, category_tag_table):
    tags_in_row = find_categories(metadata_row)
    #return tags_in_row

    # Create a dictionary of category names to boolean values
    return_categories = category_tag_table.copy()
    return_categories['has_tag'] = False

    # Iterate through the tags in the row, and set the corresponding boolean value to True
    for tag in tags_in_row:
        return_categories.loc[return_categories['Category ID'] == tag, 'has_tag'] = True

    # Return the dictionary of category names to boolean values
    category_bools = return_categories.set_index('Category Name')['has_tag'].to_dict()
    
    return category_bools
    
category_tags_to_bools(app_metadata.iloc[0], known_category_enums)
```
```
{'Multi-player': False,
 'Single-player': True,
 'Co-op': False,
 'Captions available': False,
 'Partial Controller Support': False,
 'Mods': False,
 'MMO': False,
 'Steam Achievements': False,
 'Steam Cloud': True,
 'Shared/Split Screen': False,
 'Cross-Platform Multiplayer': False,
 'Full controller support': False,
 'Steam Workshop': False,
 'VR Support': False,
 'Native Steam Controller Support': False,
 'In-App Purchases': False,
 'Online PvP': False,
 'Shared/Split Screen PvP': False,
 'Online Co-op': False,
 'Shared/Split Screen Co-op': False,
 'Remote Play on Phone': False,
 'Remote Play on Tablet': False,
 'Remote Play on TV': False,
 'Remote Play Together': False,
 'LAN PvP': False,
 'LAN Co-op': False,
 'PvP': False,
 'Tracked Controller Support': False,
 'VR Supported': False,
 'VR Only': False}
```

##### Get Languages Found in Reviews

```python
#known_languages is of type set.
def get_languages_found_in_reviews(processed_reviews, known_languages):
    # Get a list of all languages in the reviews
    languages_in_reviews = processed_reviews['language'].unique()

    # Create a dictionary of language names to boolean values
    language_bools = {language: False for language in known_languages}

    # Iterate through the languages in the reviews, and set the corresponding boolean value to True
    for language in languages_in_reviews:
        language_bools[language] = True

    return language_bools

# Example usage
get_languages_found_in_reviews(process_reviews('307110', raw_reviews_folder) , known_review_languages)
```
```
{'latam': True,
 'vietnamese': False,
 'dutch': True,
 'norwegian': True,
 'romanian': False,
 'danish': True,
 'russian': True,
 'japanese': True,
 'portuguese': True,
 'ukrainian': True,
 'french': True,
 'italian': True,
 'bulgarian': True,
 'polish': True,
 'turkish': True,
 'brazilian': True,
 'german': True,
 'hungarian': True,
 'schinese': True,
 'czech': True,
 'swedish': True,
 'koreana': True,
 'tchinese': True,
 'greek': True,
 'spanish': True,
 'thai': False,
 'finnish': False,
 'english': True}
```

#### The Primary Method

##### Generate Game and Review Entries

```python
#Generate Game entry and Top N Review entries as JSON objects for Zilliz Milvus database
def generate_game_and_review_entries(row_entry, reviews_folder, output_metadata_folder=None, output_reviews_data_folder=None, min_reviews_for_upload = 10, max_reviews_for_upload = 100):
    appid = row_entry['steam_appid']
    print("Processing appid: " + str(appid))

    # Process the reviews
    processed_reviews = process_reviews(appid, reviews_folder, min_reviews=min_reviews_for_upload)
    if processed_reviews is None:
        print("No reviews or insufficient reviews for appid: " + str(appid))
        return None

    # Pull game metadata if there actually were enough reviews.
    game_title = str(row_entry['name'])
    game_developers = str(row_entry['developers'])
    game_publishers = str(row_entry['publishers'])
    game_short_description = preprocess_text(row_entry['short_description'])
    game_detailed_description = preprocess_text(row_entry['detailed_description'])
    game_price = 0
    if not (pd.isna(row_entry['price_overview.initial'])):
        game_price = float(row_entry['price_overview.initial'])/100

    # If we reached this point, we are ready to commit to uploading this game's GameData object and up to max_reviews_for_upload ReviewData objects.
    game_data_embedding_string = game_title + ' \n ' + "Developers: " + game_developers + ' \n ' + "Publishers: " + game_publishers + ' \n ' + "Short Description: " + game_short_description
    game_data_embedding_string = shorten_text_to_max_tokens(game_data_embedding_string, token_limit=8100)
    game_data_embedding = embed(game_data_embedding_string)

    # Get the bulk review stats
    bulk_review_stats = get_bulk_review_stats(processed_reviews)

    game_total_review_count = len(processed_reviews)
    game_stats_gmean_word_count = bulk_review_stats['geometric_mean_word_count']
    game_stats_gmean_unique_word_count = bulk_review_stats['geometric_mean_unique_word_count']
    game_stats_gmean_hours_played = bulk_review_stats['geometric_mean_hours_played']
    game_stats_gmean_num_games_owned = bulk_review_stats['geometric_mean_num_games_owned']
    game_stats_gmean_author_num_reviews = bulk_review_stats['geometric_mean_author_num_reviews']
    game_stats_gmean_resonance = bulk_review_stats['geometric_mean_resonance_score']
    game_stats_first_review_recorded = str(bulk_review_stats['first_review_date'])
    game_stats_last_review_recorded = str(bulk_review_stats['last_review_date'])
    game_stats_inferred_release_year = str(bulk_review_stats['inferred_release_year'])


    # The GameData JSON object is ready to create.
    game_data = {
        'appid': int(appid), #This is the int64 identifier
        'embedding': game_data_embedding,
        'game_title': game_title,
        'inferred_release_year': game_stats_inferred_release_year,
        'game_developers': game_developers,
        'game_publishers': game_publishers,
        'game_short_description': game_short_description,
        'game_detailed_description': game_detailed_description,
        #'game_controller_support': game_controller_support,
        #'game_supported_languages': game_supported_languages,
        'game_total_review_count': game_total_review_count,
        'game_stats_gmean_word_count': game_stats_gmean_word_count,
        'game_stats_gmean_unique_word_count': game_stats_gmean_unique_word_count,
        'game_stats_gmean_hours_played': game_stats_gmean_hours_played,
        'game_stats_gmean_num_games_owned': game_stats_gmean_num_games_owned,
        'game_stats_gmean_author_num_reviews': game_stats_gmean_author_num_reviews,
        'game_stats_gmean_resonance': game_stats_gmean_resonance,
        'game_stats_first_review_recorded': game_stats_first_review_recorded,
        'game_stats_last_review_recorded': game_stats_last_review_recorded,
    }

    # Get the top N reviews
    top_n_reviews = processed_reviews.sort_values(by='resonance_score', ascending=False)[:max_reviews_for_upload]
    review_data_list = []
    for index, review in top_n_reviews.iterrows():
        review_embedding_string = (review['processed_review'])
        review_embedding = embed(shorten_text_to_max_tokens(review_embedding_string, token_limit=8100))

        review_data = {
            'review_id': int(review['recommendationid']), #This is the int64 identifier for review objects
            'embedding': review_embedding,
            'appid': appid, 
            'game_title' : game_title,
            'recommended': bool(review['voted_up']),
            'review': review['processed_review'],
            'playtime_forever': review['author.playtime_forever'],
            'review_date': review['datetime_timestamp_created'].strftime('%Y-%m-%d'),
            'resonance_score': review['resonance_score']
        }
        review_data_list.append(review_data)

    if output_metadata_folder is not None and output_reviews_data_folder is not None:
        with open(os.path.join(processed_metadata_folder, appid + '.json'), 'w') as outfile:
            json.dump(game_data, outfile)
        with open(os.path.join(processed_reviews_folder, appid + '_reviews.json'), 'w') as outfile:
            json.dump(review_data_list, outfile)

    return game_data, review_data_list
```

###### Test Run

```python
game_data, review_data_list = generate_game_and_review_entries(filtered_metadata.iloc[0], reviews_folder=raw_reviews_folder)
```
```
Processing appid: 893010
```

```python
review_data_list
```
```
...
'appid': '893010', 'game_title': "Slave's Sword", 'recommended': True, 'review': '作为在steam上玩的第一款小黄油，本作质量挺高的，妹子的形象挺不错的，不会累赘，也不会妖艳。CG很精美，剧情也不错，而且CV妹子很努力。相比较我接下来玩过的小黄油 ，这部可以达到中偏上的水平。', 'playtime_forever': 2155, 'review_date': '2019-10-31', 'resonance_score': 434.44814857686106}, {'review_id': 109773648, 'embedding': [
...],
'appid': '893010', 'game_title': "Slave's Sword", 'recommended': False, 'review': '游戏本身很平庸，唯一亮点是女主颜值。本来不用给差评的。 但是！文本/翻译有错误，牛头人遗迹迷宫的“蓝色红色”部分写反了，严重影响正常游戏，必须差评！', 'playtime_forever': 636, 'review_date': '2022-02-08', 'resonance_score': 433.72314263446765}]
```

###### Full Run

```python
cur_index = -1
for index, row in filtered_metadata.iterrows():
    cur_index += 1
    if cur_index % 100 == 0:
        print("Processed " + str(cur_index) + " games.")
        time.sleep(1) #Tiny wait which will help keep us out of rate limiting issues.

    appid = row['steam_appid']
    if(os.path.exists(os.path.join(processed_reviews_folder, appid + '_reviews.json'))):
        print("Skipping " + appid + " because reviews JSON object already exists.")
        continue

    try:
        generate_game_and_review_entries(row, reviews_folder=raw_reviews_folder, output_metadata_folder=processed_metadata_folder, output_reviews_data_folder=processed_reviews_folder, min_reviews_for_upload=20, max_reviews_for_upload=20)
    except Exception as e:
        print("Error processing " + appid + ": " + str(e))
        continue
```
```
...
No reviews for appid: 998730
Skipping 998740 because reviews JSON object already exists.
Processing appid: 998790
No reviews for appid: 998790
Processing appid: 998830
No reviews for appid: 998830
Processing appid: 998850
No reviews for appid: 998850
Processing appid: 998890
No reviews for appid: 998890
Skipping 998930 because reviews JSON object already exists.
Processing appid: 998990
No reviews for appid: 998990
Skipping 99900 because reviews JSON object already exists.
Skipping 999020 because reviews JSON object already exists.
Processing appid: 999030
No reviews for appid: 999030
Processing appid: 999040
No reviews for appid: 999040
Processing appid: 99910
No reviews for appid: 99910
Processing appid: 999190
No reviews for appid: 999190
Processing appid: 999200
No reviews for appid: 999200
Skipping 999220 because reviews JSON object already exists.
Processing appid: 999250
No reviews for appid: 999250
Processing appid: 999310
No reviews for appid: 999310
Processing appid: 999350
No reviews for appid: 999350
Skipping 999410 because reviews JSON object already exists.
Processing appid: 999430
No reviews for appid: 999430
Skipping 999640 because reviews JSON object already exists.
Skipping 999660 because reviews JSON object already exists.
Processing appid: 999840
No reviews for appid: 999840
Skipping 999860 because reviews JSON object already exists.
Processing appid: 999890
No reviews for appid: 999890
Processing appid: 999990
No reviews for appid: 999990
```

---
#### Milvus Upload

```python
# Connect to Zilliz Cloud
connections.connect(uri=URI, user=USER, password=PASSWORD, secure=True)
```

```python
##### !!!! OPTIONAL !!!! #####
##### THIS CODE DELETES EXISTING COLLECTIONS SO YOU CAN UPLOAD NEW ONES #####
# Remove collection if it already exists
if utility.has_collection(GAME_COLLECTION_NAME):
    utility.drop_collection(GAME_COLLECTION_NAME)

if utility.has_collection(REVIEW_COLLECTION_NAME):
    utility.drop_collection(REVIEW_COLLECTION_NAME)

```

```python
game_fields = [
    FieldSchema(name="appid", dtype=DataType.INT64, is_primary=True, description='Steam AppID', auto_id=False),
    FieldSchema(name="embedding", dtype=DataType.FLOAT_VECTOR, description="OpenAI embedding of title, developers, publishers, and short description", dim=DIMENSION),
    FieldSchema(name="game_title", dtype=DataType.VARCHAR, description='Game Title', max_length=512),
    FieldSchema(name="inferred_release_year", dtype=DataType.INT32, description='Inferred Release Year'),
    FieldSchema(name="game_developers", dtype=DataType.VARCHAR, description='Game Developers', max_length=512),
    FieldSchema(name="game_publishers", dtype=DataType.VARCHAR, description='Game Publishers', max_length=512),
    FieldSchema(name="game_short_description", dtype=DataType.VARCHAR, description='Short Description', max_length=10000),
    FieldSchema(name="game_detailed_description", dtype=DataType.VARCHAR, description='Detailed Description', max_length=50000),
    FieldSchema(name="game_controller_support", dtype=DataType.VARCHAR, description='Controller Support', max_length=512),
    FieldSchema(name="game_supported_languages", dtype=DataType.VARCHAR, description='Supported Languages', max_length=512),
    FieldSchema(name="game_total_review_count", dtype=DataType.INT32, description='Total Review Count'),
    FieldSchema(name="game_stats_gmean_word_count", dtype=DataType.FLOAT, description='GMean Reviews Word Count'),
    FieldSchema(name="game_stats_gmean_unique_word_count", dtype=DataType.FLOAT, description='GMean Reviews Unique Word Count'),
    FieldSchema(name="game_stats_gmean_hours_played", dtype=DataType.FLOAT, description='GMean Reviews Hours Played'),
    FieldSchema(name="game_stats_gmean_author_num_reviews", dtype=DataType.FLOAT, description='GMean Reviews Author Num Reviews'),
    FieldSchema(name="game_stats_gmean_resonance", dtype=DataType.FLOAT, description='GMean Reviews Resonance'),
    FieldSchema(name="game_stats_first_review_recorded", dtype=DataType.VARCHAR, description='Date First Review Recorded', max_length=200),
    FieldSchema(name="game_stats_last_review_recorded", dtype=DataType.VARCHAR, description='Date Most Recent Review Recorded', max_length=200),
]

game_schema = CollectionSchema(fields=game_fields, description = "Steam Game Metadata")
game_collection = Collection(name=GAME_COLLECTION_NAME, schema=game_schema)

# Create an index for the collection.
game_index_params = {
    'index_type': 'AUTOINDEX',
    'metric_type': 'L2',
    'params': {}
}
game_collection.create_index(field_name="embedding", index_params=game_index_params)
```
```
Status(code=0, message=)
```

```python
review_fields = [
    FieldSchema(name="review_id", dtype=DataType.INT64, is_primary=True, description='Steam Review ID', auto_id=False),
    FieldSchema(name="embedding", dtype=DataType.FLOAT_VECTOR, description="OpenAI embedding of game title, date, and review", dim=DIMENSION),
    FieldSchema(name="appid", dtype=DataType.INT64, description='Game Steam AppID'),
    FieldSchema(name="game_title", dtype=DataType.VARCHAR, description='Game Title', max_length=2000),
    FieldSchema(name="review", dtype=DataType.VARCHAR, description='Review Text', max_length=50000),
    FieldSchema(name="playtime_forever", dtype=DataType.INT32, description='Playtime Forever'),
    FieldSchema(name="review_date", dtype=DataType.VARCHAR, description='Review Date', max_length=256),
    FieldSchema(name="resonance_score", dtype=DataType.FLOAT, description='Resonance Score')
]

review_schema = CollectionSchema(fields=review_fields, description = "Steam Review Metadata")
review_collection = Collection(name=REVIEW_COLLECTION_NAME, schema=review_schema)

# Create an index for the collection.
review_index_params = {
    'index_type': 'AUTOINDEX',
    'metric_type': 'L2',
    'params': {}
}
review_collection.create_index(field_name="embedding", index_params=review_index_params)
```
```
Status(code=0, message=)
```

##### Milvus Upload Methods and Code

```python
def game_data_json_to_df(appid, metadata_folder):
    if not os.path.exists(os.path.join(metadata_folder, appid + '.json')):
        return None
    
    game_metadata = pd.json_normalize(json.load(open(os.path.join(metadata_folder, appid + '.json'))))
    perfectly_formatted_game_metadata = {
        'appid': game_metadata['appid'].values[0],
        'embedding': np.array(game_metadata['embedding'].tolist(), dtype=np.float32).tolist(),
        'game_title': game_metadata['game_title'].values[0],
        'inferred_release_year': game_metadata['inferred_release_year'].astype(np.int32).values[0],
        'game_developers': game_metadata['game_developers'].values[0],
        'game_publishers': game_metadata['game_publishers'].values[0],
        'game_short_description': game_metadata['game_short_description'].values[0],
        'game_detailed_description': game_metadata['game_detailed_description'].values[0],
        'game_controller_support': game_metadata['game_controller_support'].values[0],
        'game_supported_languages': game_metadata['game_supported_languages'].values[0],
        'game_total_review_count': game_metadata['game_total_review_count'].astype(np.int32).values[0],
        'game_stats_gmean_word_count': game_metadata['game_stats_gmean_word_count'].astype(np.float32).values[0],
        'game_stats_gmean_unique_word_count': game_metadata['game_stats_gmean_unique_word_count'].astype(np.float32).values[0],
        'game_stats_gmean_hours_played': game_metadata['game_stats_gmean_hours_played'].astype(np.float32).values[0],
        'game_stats_gmean_author_num_reviews': game_metadata['game_stats_gmean_author_num_reviews'].astype(np.float32).values[0],
        'game_stats_gmean_resonance': game_metadata['game_stats_gmean_resonance'].astype(np.float32).values[0],
        'game_stats_first_review_recorded': game_metadata['game_stats_first_review_recorded'].values[0],
        'game_stats_last_review_recorded': game_metadata['game_stats_last_review_recorded'].values[0]
    }   

    return pd.DataFrame(perfectly_formatted_game_metadata)
```

```python
def review_data_json_to_df(appid, reviews_folder):
    if not os.path.exists(os.path.join(reviews_folder, appid + '_reviews.json')):
        return None
    
    unformatted_reviews = pd.json_normalize(json.load(open(os.path.join(processed_reviews_folder, appid + '_reviews.json'))))

    perfectly_formatted_reviews = pd.DataFrame({
        'review_id': unformatted_reviews['review_id'].astype(np.int64),
        'embedding': unformatted_reviews['embedding'],
        'appid': unformatted_reviews['appid'].astype(np.int64),
        'game_title': unformatted_reviews['game_title'],
        'review': unformatted_reviews['review'],
        'playtime_forever': unformatted_reviews['playtime_forever'].astype(np.int32),
        'review_date': unformatted_reviews['review_date'],
        'resonance_score': unformatted_reviews['resonance_score'].astype(np.float32)})
    
    return perfectly_formatted_reviews
    
    
```

```python
# Find all appids that will be uploaded
appids_to_upload = []
for filename in os.listdir(processed_metadata_folder):
    if filename.endswith(".json"):
        appid = filename.split('.')[0]
        appids_to_upload.append(appid)

# Keep a list of appids that already have been uploaded
appids_already_uploaded = []
```

```python
#Grab the JSON file for each game's metadata and reviews bundle. Upload the metadata to the game collection and the reviews to the review collection.
num_processed = 0
cur_appid = ''
for appid in appids_to_upload:
    cur_index += 1
    cur_appid = str(appid)

    if appid in appids_already_uploaded:
        print("Skipping " + appid + " because it has already been uploaded.")
        continue

    game_metadata_df = game_data_json_to_df(appid, processed_metadata_folder)
    if game_metadata_df is None:
        print("Skipping " + appid + " because it has no metadata.")
        continue
    review_df = review_data_json_to_df(appid, processed_reviews_folder)
    if review_df is None:
        print("Skipping " + appid + " because it has no reviews.")
        continue

    try:
        game_collection.insert(game_metadata_df)
        review_collection.insert(review_df)
        appids_already_uploaded.append(appid)
        print("Successfully uploaded " + appid + " to Milvus.")

        num_processed += 1
        if num_processed % 100 == 0:
            print("Processed " + str(num_processed) + " games. Flushing collections.")
            game_collection.flush()
            review_collection.flush()

    except Exception as e:
        print("Failed to upload " + appid + " to Milvus.")
        print(e)
        continue
```
```
...
Successfully uploaded 994340 to Milvus.
Successfully uploaded 994500 to Milvus.
Successfully uploaded 994670 to Milvus.
Successfully uploaded 994730 to Milvus.
Successfully uploaded 995050 to Milvus.
Successfully uploaded 995070 to Milvus.
Successfully uploaded 995230 to Milvus.
Successfully uploaded 995460 to Milvus.
Successfully uploaded 995980 to Milvus.
Successfully uploaded 996080 to Milvus.
Successfully uploaded 996380 to Milvus.
Successfully uploaded 996450 to Milvus.
Successfully uploaded 996580 to Milvus.
Successfully uploaded 996770 to Milvus.
Successfully uploaded 99700 to Milvus.
Successfully uploaded 997010 to Milvus.
Successfully uploaded 997070 to Milvus.
Successfully uploaded 997380 to Milvus.
Successfully uploaded 997720 to Milvus.
Successfully uploaded 9980 to Milvus.
Successfully uploaded 998740 to Milvus.
Successfully uploaded 998930 to Milvus.
Successfully uploaded 99900 to Milvus.
Successfully uploaded 999020 to Milvus.
Successfully uploaded 999220 to Milvus.
Successfully uploaded 999410 to Milvus.
Successfully uploaded 999640 to Milvus.
Successfully uploaded 999660 to Milvus.
Successfully uploaded 999860 to Milvus.
```

##### Milvus Search Methods Code

```python
# You need to run this before you run searches. 
# By default, Zilliz keeps the index on disk. You have to pull it into memory for these queries.
review_collection.load()
game_collection.load()
```

###### Milvus Search Reviews
```python
def milvus_search_reviews(query_text, results_limit = 100):
    #Search parameters for the index
    search_params={
        "metric_type": "L2"
    }

    results=review_collection.search(
        data=[embed(query_text)],  # Embeded search value
        anns_field="embedding",  # Search across embeddings
        param=search_params,
        limit=results_limit,  # Limit to five results per search
        output_fields=['appid', 'game_title', 'review', 'playtime_forever', 'review_date', 'resonance_score']  # Include title field in result
    )

    ret=[]
    for hit in results[0]:
        row=[]
        row.extend([hit.id, 
                    hit.score, 
                    hit.entity.get('appid'),
                    hit.entity.get('game_title'),
                    hit.entity.get('review'),
                    hit.entity.get('playtime_forever'),
                    hit.entity.get('review_date'),
                    hit.entity.get('resonance_score')])  # Get the id, distance, and title for the results
        ret.append(row)

    ret_df = pd.DataFrame(ret, columns=['id', 'score', 'appid', 'game_title', 'review', 'playtime_forever', 'review_date', 'resonance_score'])
    return ret_df

# Example search
# milvus_search_reviews("An underground crew sim game like Volcanoids where you and a crew pilot a great digging machine")
```

###### Rank Milvus Reviews Results
```python
def rank_milvus_reviews_results(unranked_results, max_games_to_return = 20):

    # Version 1: Divide resonance by the distance to the query (called 'score' when returned from Milvus)
    # We just group by appid and take the mean of the new weighted score to get a ranked order to return.
    # unranked_results['weighted_score'] = unranked_results['resonance_score'] / unranked_results['score']
    # unranked_results.sort_values(by=['weighted_score'], ascending=False)
    # ranked_results = unranked_results.groupby('appid').mean(numeric_only=True).sort_values(by=['weighted_score'], ascending=[False])
    # return ranked_results[:max_games_to_return]

    # Version 2: Take the distance `score` provided by Milvus and subtract it from 1. We will call this 'proximity_score'.
    # Normalize the proximity_score to be between 0 and 1 by subtracting the minimum proximity_score and dividing by the range, calling it 'normalized_proximity_score'.
    # Normalize the 'resonance_score' to be between 0 and 1 by subtracting the minimum resonance_score and dividing by the range, calling it 'normalized_resonance_score'.
    # Add the normalized_proximity_score and normalized_resonance_score to get a new 'weighted_score'.
    unranked_results['proximity_score'] = 1 - unranked_results['score']
    unranked_results['normalized_proximity_score'] = (unranked_results['proximity_score'] - unranked_results['proximity_score'].min()) / (unranked_results['proximity_score'].max() - unranked_results['proximity_score'].min())
    unranked_results['normalized_resonance_score'] = (unranked_results['resonance_score'] - unranked_results['resonance_score'].min()) / (unranked_results['resonance_score'].max() - unranked_results['resonance_score'].min())
    unranked_results['weighted_score'] = unranked_results['normalized_proximity_score'] + unranked_results['normalized_resonance_score']
    ranked_results = unranked_results.groupby('appid').mean(numeric_only=True).sort_values(by=['weighted_score'], ascending=[False])
    return ranked_results[:max_games_to_return]
```

```python
reviews_query = "A souls-like game set in space"
reviews_query_results = milvus_search_reviews(reviews_query)
ranked_reviews_query_results = rank_milvus_reviews_results(reviews_query_results)
ranked_reviews_query_results
```
```
                   id     score  playtime_forever  resonance_score   
appid                                                                
774201   6.224670e+07  0.279498       1578.000000      1889.875977  \
628670   9.551193e+07  0.280870       1358.500000      1697.333008   
727020   9.414053e+07  0.246987        101.000000        69.962433   
1419160  1.166356e+08  0.299149       2524.000000      1761.476562   
425340   2.084973e+07  0.279705       1446.000000       877.063416   
531050   5.420511e+07  0.292333       1149.000000      1124.667114   
465020   4.787556e+07  0.292442        837.000000      1103.312012   
1138850  1.116801e+08  0.284391       1198.000000       814.712524   
283310   1.235313e+08  0.272947       1166.000000       420.407501   
732240   4.936711e+07  0.297588       2736.000000      1221.312744   
742250   4.707196e+07  0.275345        506.000000       455.673309   
1714080  1.087003e+08  0.274964        418.000000       427.646057   
251850   7.561020e+06  0.280475        496.000000       610.786621   
279900   1.959742e+07  0.288597       2196.000000       872.393372   
857980   5.104177e+07  0.298959        810.000000      1216.305054   
906100   1.298268e+08  0.291634        894.000000       957.818909   
1748230  1.248778e+08  0.276436        562.000000       435.677856   
269690   2.684175e+07  0.291128       2357.285714       927.591919   
300060   2.424009e+07  0.290391        940.000000       891.873596   
824070   5.201203e+07  0.295880       1236.000000      1060.372314   

         proximity_score  normalized_proximity_score   
appid                                                  
774201          0.720502                    0.389696  \
628670          0.719130                    0.363955   
727020          0.753013                    1.000000   
1419160         0.700851                    0.020819   
425340          0.720295                    0.385818   
531050          0.707667                    0.148767   
465020          0.707558                    0.146720   
1138850         0.715609                    0.297845   
283310          0.727053                    0.512681   
732240          0.702412                    0.050129   
742250          0.724655                    0.467662   
1714080         0.725036                    0.474824   
251850          0.719525                    0.371362   
279900          0.711403                    0.218897   
857980          0.701041                    0.024393   
906100          0.708366                    0.161885   
1748230         0.723564                    0.447177   
269690          0.708872                    0.171396   
300060          0.709609                    0.185216   
824070          0.704120                    0.082191   

         normalized_resonance_score  weighted_score  
appid                                                
774201                     1.000000        1.389696  
628670                     0.894202        1.258157  
727020                     0.000000        1.000000  
1419160                    0.929448        0.950266  
425340                     0.443483        0.829302  
531050                     0.579536        0.728303  
465020                     0.567801        0.714522  
1138850                    0.409223        0.707068  
283310                     0.192561        0.705242  
732240                     0.632640        0.682769  
742250                     0.211939        0.679601  
1714080                    0.196539        0.671363  
251850                     0.297170        0.668533  
279900                     0.440917        0.659814  
857980                     0.629889        0.654282  
906100                     0.487856        0.649742  
1748230                    0.200952        0.648129  
269690                     0.471247        0.642643  
300060                     0.451621        0.636837  
824070                     0.544207        0.626398  
```

```python
reviews_query_results
```
```
           id     score    appid                game_title   
0    94140529  0.246987   727020         Arcade Moonlander  \
1    54077916  0.257831   742250  OPUS: Rocket of Whispers   
2    30095093  0.265483   586240            Soul Searching   
3    28907340  0.266326   350480           Tales of Cosmos   
4    30019953  0.268251   586240            Soul Searching   
..        ...       ...      ...                       ...   
95  134721741  0.299676  1646850             SpaceBourne 2   
96   39568642  0.299807   347000                InnerSpace   
97   29959956  0.300156   586240            Soul Searching   
98   17490393  0.300221   293240               Cosmochoria   
99   48410255  0.300258   312230         Spirits of Xanadu   

                                               review  playtime_forever   
0   The souls-like of space games. 10/10 would rag...               101  \
1   This story is shockingly beautiful and moving....               509   
2   Very impressive and original little game. You ...               218   
3   A really inventive little puzzle game. The art...               200   
4   Here we have a lovely indie game filled with m...               334   
..                                                ...               ...   
95  Man. This game is BOSS on so many levels. Firs...              3593   
96   I had a great time. The visuals were amazing ...               329   
97  Wow, game is just finished now and I didn't re...               211   
98  I thought this was just like a small arcade ga...               770   
99  Spirits of Xanadu is a quite interesting indie...               270   

   review_date  resonance_score  proximity_score  normalized_proximity_score   
0   2021-06-21        69.962433         0.753013                    1.000000  \
1   2019-07-13       416.946838         0.742169                    0.796441   
2   2017-02-22       313.997925         0.734517                    0.652784   
3   2017-01-01       240.679779         0.733674                    0.636965   
4   2017-02-18       264.565796         0.731749                    0.600832   
..         ...              ...              ...                         ...   
95  2023-03-15       967.487427         0.700324                    0.010922   
96  2018-01-23       416.550720         0.700193                    0.008466   
97  2017-02-16       291.759583         0.699844                    0.001907   
98  2015-08-11       772.653625         0.699779                    0.000701   
99  2019-01-18       623.110474         0.699742                    0.000000   

    normalized_resonance_score  weighted_score  
0                     0.000000        1.000000  
1                     0.190660        0.987101  
2                     0.134092        0.786876  
3                     0.093805        0.730770  
4                     0.106930        0.707762  
..                         ...             ...  
95                    0.493169        0.504091  
96                    0.190442        0.198908  
97                    0.121872        0.123779  
98                    0.386112        0.386813  
99                    0.303942        0.303942  

[100 rows x 12 columns]
```

```python
reviews_query_results.iloc[3]['review']
```
```
"Booted up the game, set on hard, Can't even get past the first cutscene 10/10 Just like Dark Souls"
```

```python
ranked_reviews_query_results.groupby('appid').mean(numeric_only=True).sort_values(by=['weighted_score'], ascending=[False])[:20]
```
```
                   id     score  playtime_forever  resonance_score   
appid                                                                
1225590  1.132039e+08  0.298383       1426.000000       723.724304  \
1042490  9.300141e+07  0.321416        833.428571       969.395935   
460700   2.828189e+07  0.330446       1296.000000      1050.442505   
1338840  1.321035e+08  0.317621        767.000000       600.455322   
983350   5.569511e+07  0.319737        817.000000       588.272156   
337720   4.032676e+07  0.326598        819.400000       724.461243   
558420   4.518300e+07  0.330360       1173.000000       782.769592   
848080   1.155874e+08  0.321494       1153.000000       573.879333   
1128140  7.826477e+07  0.314887        346.000000       334.218781   
323280   2.903599e+07  0.316864        253.500000       370.541077   
737050   8.425342e+07  0.336141      16325.000000       781.606567   
1211930  1.150154e+08  0.316050        745.750000       333.276978   
1614270  1.166442e+08  0.335455        874.000000       728.126221   
450500   2.930053e+07  0.331361        710.500000       639.968994   
572700   8.327168e+07  0.330411       2086.000000       581.302429   
814360   7.342152e+07  0.326167       1008.333333       417.223480   
990920   1.154903e+08  0.328725       2349.000000       467.232880   
919330   9.765654e+07  0.320544        207.000000       285.263947   
747690   5.795683e+07  0.332405        757.000000       530.870605   
411560   3.851143e+07  0.330828       1521.000000       479.569519   

         proximity_score  normalized_proximity_score   
appid                                                  
1225590         0.701617                    0.762605  \
1042490         0.678584                    0.318781   
460700          0.669554                    0.144799   
1338840         0.682379                    0.391910   
983350          0.680263                    0.351144   
337720          0.673402                    0.218945   
558420          0.669640                    0.146456   
848080          0.678506                    0.317282   
1128140         0.685113                    0.444598   
323280          0.683136                    0.406501   
737050          0.663859                    0.035062   
1211930         0.683950                    0.422179   
1614270         0.664545                    0.048274   
450500          0.668639                    0.127161   
572700          0.669589                    0.145468   
814360          0.673833                    0.227245   
990920          0.671275                    0.177959   
919330          0.679456                    0.335580   
747690          0.667595                    0.107045   
411560          0.669172                    0.137440   

         normalized_resonance_score  weighted_score  
appid                                                
1225590                    0.609199        1.371804  
1042490                    0.831110        1.149891  
460700                     0.904318        1.049117  
1338840                    0.497852        0.889762  
983350                     0.486847        0.837991  
337720                     0.609864        0.828809  
558420                     0.662533        0.808990  
848080                     0.473846        0.791128  
1128140                    0.257364        0.701963  
323280                     0.290174        0.696675  
737050                     0.661483        0.696545  
1211930                    0.256514        0.678693  
1614270                    0.613175        0.661448  
450500                     0.533544        0.660705  
572700                     0.480551        0.626019  
814360                     0.332341        0.559586  
990920                     0.377514        0.555473  
919330                     0.213144        0.548725  
747690                     0.434997        0.542042  
411560                     0.388658        0.526098  
```

###### Milvus - Search Games By Text
```python
def milvus_search_games_by_text(query_text, results_limit = 100):
    #Search parameters for the index
    search_params={
        "metric_type": "L2"
    }

    results=game_collection.search(
        data=[embed(query_text)],  # Embeded search value
        anns_field="embedding",  # Search across embeddings
        param=search_params,
        limit=results_limit,  # Limit to five results per search
        output_fields=['appid', 
                       'game_title', 
                       'inferred_release_year',
                       'game_developers', 
                       'game_publishers', 
                       'game_short_description', 
                       'game_detailed_description',
                       'game_controller_support',
                       'game_supported_languages',
                       'game_total_review_count',
                       'game_stats_gmean_word_count',
                       'game_stats_gmean_unique_word_count',
                       'game_stats_gmean_hours_played',
                       'game_stats_gmean_author_num_reviews',
                       'game_stats_gmean_resonance',
                       'game_stats_first_review_recorded',
                       'game_stats_last_review_recorded'
        ] 
    )

    ret=[]
    for hit in results[0]:
        row=[]
        row.extend([hit.id,
                    hit.score,
                    hit.entity.get('appid'),
                    hit.entity.get('game_title'),
                    hit.entity.get('inferred_release_year'),
                    hit.entity.get('game_developers'),
                    hit.entity.get('game_publishers'),
                    hit.entity.get('game_short_description'),
                    hit.entity.get('game_detailed_description'),
                    hit.entity.get('game_controller_support'),
                    hit.entity.get('game_supported_languages'),
                    hit.entity.get('game_total_review_count'),
                    hit.entity.get('game_stats_gmean_word_count'),
                    hit.entity.get('game_stats_gmean_unique_word_count'),
                    hit.entity.get('game_stats_gmean_hours_played'),
                    hit.entity.get('game_stats_gmean_author_num_reviews'),
                    hit.entity.get('game_stats_gmean_resonance'),
                    hit.entity.get('game_stats_first_review_recorded'),
                    hit.entity.get('game_stats_last_review_recorded')
                    ])  # Get the id, distance, and title for the results
        ret.append(row)

    ret_df = pd.DataFrame(ret, columns=['id', 
                                        'score', 
                                        'appid', 
                                        'game_title', 
                                        'inferred_release_year', 
                                        'game_developers', 
                                        'game_publishers', 
                                        'game_short_description', 
                                        'game_detailed_description', 
                                        'game_controller_support', 
                                        'game_supported_languages', 
                                        'game_total_review_count', 
                                        'game_stats_gmean_word_count', 
                                        'game_stats_gmean_unique_word_count', 
                                        'game_stats_gmean_hours_played', 
                                        'game_stats_gmean_author_num_reviews', 
                                        'game_stats_gmean_resonance', 
                                        'game_stats_first_review_recorded', 
                                        'game_stats_last_review_recorded'])
    
    return ret_df
```

```python
game_search_query = "An underground crew sim game like Volcanoids where you and a crew pilot a great digging machine"
game_search_results = milvus_search_games_by_text(game_search_query)
game_search_results.head(10)
```
```
        id     score    appid                    game_title   
0   951440  0.258590   951440                    Volcanoids  \
1   311910  0.278432   311910  DIG IT! - A Digger Simulator   
2   273820  0.281332   273820  Mining & Tunneling Simulator   
3  1523510  0.283450  1523510     Cave Digger 2: Dig Harder   
4   503340  0.286335   503340             Dig 4 Destruction   
5  1526380  0.296458  1526380        Excavator Simulator VR   
6  1441790  0.301152  1441790               Miner: Dig Deep   
7   844380  0.303104   844380                Cave Digger VR   
8   350620  0.310154   350620                     Sandmason   
9  1528050  0.312462  1528050                     Underland   

   inferred_release_year                            game_developers   
0                   2019                              ['Volcanoid']  \
1                   2014                        ['Cape Copenhagen']   
2                   2014  ['United Independent Entertainment GmbH']   
3                   2021                                 ['VRKiwi']   
4                   2016                           ['COLOPL, Inc.']   
5                   2021                             ['MrJohnWeez']   
6                   2020                        ['Substance Games']   
7                   2019                                 ['VRKiwi']   
8                   2015                               ['GoodVole']   
9                   2021                       ['Minicactus Games']   

                             game_publishers   
0                              ['Volcanoid']  \
1                        ['rondomedia GmbH']   
2  ['United Independent Entertainment GmbH']   
3                                 ['VRKiwi']   
4                           ['COLOPL, Inc.']   
5                             ['MrJohnWeez']   
6                        ['Substance Games']   
7                                 ['VRKiwi']   
8                               ['GoodVole']   
9                       ['Minicactus Games']   

                              game_short_description   
0  A base-building open-world survival shooter th...  \
1  Do you love that moment when the engine starts...   
2  Blast rocks, drill tunnels and force your way ...   
3  Cave Digger 2 is an action-adventure game in w...   
4  The first of its kind! A first for VR! An inte...   
5  Ready for work? Excavator Simulator VR gives a...   
6  A cozy mining platformer game. Dig deep into t...   
7  Cave Digger is a virtual reality mining game i...   
8  You are a mine worker, stuck deep in an underg...   
9  Use the elements at your disposal, such as exc...   

                           game_detailed_description game_controller_support   
0   JOIN THE COMMUNITY About the Game Volcanoids ...                    full  \
1   Do you love that moment when the engine start...                     nan   
2  Blast rocks, drill tunnels and force your way ...                     nan   
3   Cave Digger 2: Dig Harder The player is a pro...                     nan   
4  The first of its kind! A first for VR! An inte...                     nan   
5  Excavator Simulator VR is a short showcase of ...                     nan   
6  Miner: Dig Deep is back! Originally released o...                    full   
7   Buy the sequel now! About the Game Cave Digge...                     nan   
8  You are a mine worker, stuck deep in an underg...                     nan   
9  “The astronauts who set out in search of a new...                     nan   

                            game_supported_languages  game_total_review_count   
0  English * , French, German, Spanish - Spain, C...                     2137  \
1  English, French, Italian, German, Spanish - Sp...                       44   
2  English, French, German, Dutch, Hungarian, Polish                       23   
3  English * , French, Italian, German, Spanish -...                       38   
4                                  English, Japanese                       33   
5      English * * languages with full audio support                       35   
6                English, Traditional Chinese, Czech                       83   
7  English * , Simplified Chinese, German, Spanis...                       28   
8      English * * languages with full audio support                       21   
9  English, Portuguese - Brazil, Simplified Chine...                       48   

   game_stats_gmean_word_count  game_stats_gmean_unique_word_count   
0                    41.064610                           33.926132  \
1                    58.432434                           43.893841   
2                    35.346569                           29.079157   
3                    52.279503                           41.361446   
4                    48.971066                           40.872799   
5                    51.829411                           41.994915   
6                    41.821426                           34.508492   
7                    56.027969                           42.261047   
8                    47.921616                           40.743103   
9                    36.290207                           31.359461   

   game_stats_gmean_hours_played  game_stats_gmean_author_num_reviews   
0                     822.012756                             9.773528  \
1                     332.759827                             7.158288   
2                      97.328064                             5.854546   
3                     255.078659                            14.961064   
4                     100.537056                            20.299574   
5                      46.157043                             7.974756   
6                     293.954529                             9.912363   
7                     131.971619                            17.767305   
8                     180.303604                            43.278309   
9                     116.932762                            32.565155   

   game_stats_gmean_resonance game_stats_first_review_recorded   
0                  280.445740                       2019-01-29  \
1                  244.168518                       2014-10-15   
2                   90.852608                       2014-03-05   
3                  180.756973                       2021-09-14   
4                  111.164955                       2016-08-14   
5                   58.848339                       2021-03-30   
6                  176.864395                       2020-11-28   
7                  122.069260                       2019-11-28   
8                  162.004105                       2015-05-10   
9                   91.830132                       2021-02-05   

  game_stats_last_review_recorded  
0                      2023-03-19  
1                      2023-01-07  
2                      2022-04-23  
3                      2023-02-18  
4                      2020-08-30  
5                      2023-02-07  
6                      2023-03-04  
7                      2022-12-06  
8                      2018-08-05  
9                      2023-01-03  
```


##### Experiments in Ranking

```python
game_search_results['weighted_score'] = game_search_results['game_stats_gmean_resonance'] / game_search_results['score']
game_search_results.sort_values(by=['weighted_score'], ascending=False).head(10)
```
```
        id     score   appid                      game_title   
53  340490  0.350658  340490                      Subterrain  \
60  250520  0.351986  250520                       UnderRail   
55  704510  0.350688  704510                  Mercury Fallen   
12  268650  0.331843  268650                 From the Depths   
21  241560  0.334940  241560                       The Crew™   
15  446790  0.332983  446790           Diluvion: Resubmerged   
0   951440  0.258611  951440                      Volcanoids   
56  422810  0.351113  422810  River City Ransom: Underground   
46  732050  0.348582  732050                    Voxel Tycoon   
16  656350  0.333245  656350                       UnderMine   

    inferred_release_year                                    game_developers   
53                   2015                                  ['Pixellore Inc']  \
60                   2013                               ['Stygian Software']   
55                   2017                              ['Nitrous Butterfly']   
12                   2014                           ['Brilliant Skies Ltd.']   
21                   2014  ['Ivory Tower in collaboration with Ubisoft Re...   
15                   2017                                 ['Arachnid Games']   
0                    2019                                      ['Volcanoid']   
56                   2017                          ['Conatus Creative Inc.']   
46                   2021                                   ['Voxel Tycoon']   
16                   2019                                        ['Thorium']   

                    game_publishers   
53                ['Pixellore Inc']  \
60             ['Stygian Software']   
55            ['Nitrous Butterfly']   
12         ['Brilliant Skies Ltd.']   
21                      ['Ubisoft']   
15  ['Good Shepherd Entertainment']   
0                     ['Volcanoid']   
56        ['Conatus Creative Inc.']   
46                 ['Voxel Tycoon']   
16                      ['Thorium']   

                               game_short_description   
53  Uncompromising Sci-Fi survival on Mars! Manage...  \
60  Underrail is an old school turn-based isometri...   
55  Sci-fi colony management with a focus on expan...   
12  Over 1000 unique components allow you to build...   
21  The Crew is a revolutionary action-driving MMO...   
15  Diluvion is a 3D deep sea, Jules Verne inspire...   
0   A base-building open-world survival shooter th...   
56  Alex and Ryan must take to the streets to help...   
46  Voxel Tycoon — a management sim set in the inf...   
16  An action-adventure roguelike with a bit of RP...   

                            game_detailed_description game_controller_support   
53   Check Out Subterrain: Mines of Titan Localiza...                    full  \
60  Underrail is an old school turn-based isometri...                     nan   
55   The game is in the early access alpha stage a...                     nan   
12   Workshop for vehicles, planets and mods Share...                     nan   
21   Ultimate Edition Explore the first realistic ...                     nan   
15   Fleet Edition The Special Fleet Edition of Di...                    full   
0    JOIN THE COMMUNITY About the Game Volcanoids ...                    full   
56  One of the best beat'em ups of the NES era is ...                    full   
46   Join our Discord community! About the Game Vo...                     nan   
16   Delve deep into the UnderMine and discover it...                    full   

                             game_supported_languages   
53  English, Russian, Traditional Chinese, French,...  \
60                                            English   
55                                            English   
12  English * , Simplified Chinese, Russian * lang...   
21  English * , French * , Italian * , German * , ...   
15  English * , French, German, Russian, Polish, H...   
0   English * , French, German, Spanish - Spain, C...   
56              English, Japanese, Simplified Chinese   
46  English, Russian, French, Italian, German, Spa...   
16  English, German, Portuguese - Brazil, French, ...   

    game_total_review_count  game_stats_gmean_word_count   
53                      376                    88.728485  \
60                     2624                    62.708294   
55                      103                    59.785454   
12                     5273                    48.421875   
21                     3450                    54.161415   
15                      426                    87.763252   
0                      2137                    41.064610   
56                      878                    75.789093   
46                      493                    53.097786   
16                     2252                    42.088860   

    game_stats_gmean_unique_word_count  game_stats_gmean_hours_played   
53                           65.810638                    1735.949219  \
60                           48.841679                    5233.362305   
55                           47.691986                    1966.279419   
12                           38.942081                   11350.633789   
21                           42.438812                    2118.023926   
15                           64.965004                     575.622925   
0                            33.926132                     822.012756   
56                           57.371391                     845.959167   
46                           42.675560                    1309.177002   
16                           34.656281                    1973.779907   

    game_stats_gmean_author_num_reviews  game_stats_gmean_resonance   
53                            19.752880                  502.912292  \
60                            12.651764                  453.529968   
55                            11.604262                  404.807861   
12                             7.288432                  380.996368   
21                             8.094926                  383.709717   
15                            16.253452                  362.638428   
0                              9.773528                  280.445740   
56                            12.604413                  366.617340   
46                            10.656942                  355.136597   
16                            10.026137                  337.838165   

   game_stats_first_review_recorded game_stats_last_review_recorded   
53                       2015-05-03                      2022-12-25  \
60                       2013-09-25                      2023-03-18   
55                       2017-10-16                      2023-02-26   
12                       2014-08-08                      2023-03-20   
21                       2014-08-25                      2023-03-15   
15                       2017-02-02                      2022-12-30   
0                        2019-01-29                      2023-03-19   
56                       2017-02-27                      2023-02-28   
46                       2021-04-15                      2023-03-19   
16                       2019-08-20                      2023-03-11   

    weighted_score  
53     1434.194515  
60     1288.487162  
55     1154.325665  
12     1148.121884  
21     1145.607267  
15     1089.059245  
0      1084.432725  
56     1044.157275  
46     1018.804821  
16     1013.783755  
```

```python
#Get the rank of score for game search results
game_search_results['score'].rank(ascending=False)
```
```
0     100.0
1      99.0
2      98.0
3      97.0
4      96.0
      ...  
95      5.0
96      4.0
97      3.0
98      2.0
99      1.0
Name: score, Length: 100, dtype: float64
```

```python
#Normalize 'game_stats_gmean_resonance' to a 0-1 range for game search results
game_search_results['game_stats_gmean_resonance'].apply(lambda x: (x - game_search_results['game_stats_gmean_resonance'].min()) / (game_search_results['game_stats_gmean_resonance'].max() - game_search_results['game_stats_gmean_resonance'].min()))
```
```
0     0.519452
1     0.441090
2     0.109913
3     0.304115
4     0.153790
        ...   
95    0.197985
96    0.651345
97    0.629373
98    0.352430
99    0.594813
Name: game_stats_gmean_resonance, Length: 100, dtype: float64
```

```python
rank_milvus_reviews_results(milvus_search_reviews("An underground crew sim game like Volcanoids where you and a crew pilot a great digging machine"))
```
```
                   id     score  playtime_forever  resonance_score   
appid                                                                
951440   1.016941e+08  0.295961       2274.928571      1444.538696  \
704510   3.582510e+07  0.331666      84297.000000      1124.269165   
577670   1.050246e+08  0.329354       1529.000000      1067.702271   
650350   3.413721e+07  0.331259       2712.000000      1007.544922   
65270    1.713078e+07  0.312437       2343.000000       882.987671   
200370   7.999551e+06  0.324779       1237.000000       910.839783   
1392650  1.245431e+08  0.280990       2730.000000       775.358521   
252410   6.280992e+07  0.322792        928.800000       880.562012   
513480   3.283160e+07  0.315348       1833.000000       749.715637   
321830   1.257264e+07  0.319768        878.500000       710.801025   
35480    2.801153e+07  0.329618       1236.333333       725.025818   
376250   2.523074e+07  0.326012       1437.000000       718.975464   
223430   1.151511e+07  0.336783        530.000000       699.428284   
507120   5.073322e+07  0.332280       1338.000000       576.899841   
593070   4.857250e+07  0.332408        657.250000       521.930176   
329310   4.160821e+07  0.313059       1200.000000       484.115662   
311910   3.929315e+07  0.318942        836.454545       479.815521   
567370   4.903708e+07  0.333333        518.500000       495.962036   
402750   7.280439e+07  0.330902        386.000000       475.553040   
211180   1.025627e+07  0.327072        776.000000       464.098877   

         weighted_score  
appid                    
951440      4907.673136  
704510      3389.761641  
577670      3241.804693  
650350      3041.564212  
65270       2826.126540  
200370      2804.494313  
1392650     2759.376463  
252410      2727.688753  
513480      2377.425183  
321830      2238.869901  
35480       2207.880971  
376250      2205.361700  
223430      2076.791444  
507120      1736.188155  
593070      1569.854004  
329310      1546.403899  
311910      1492.764885  
567370      1486.215233  
402750      1437.141197  
211180      1418.952154  
```

```python
search_params={
        "metric_type": "L2"
}
```

```python
test_results=review_collection.search(
    data=[embed("An underground crew sim game like Volcanoids where you and a crew pilot a great digging machine")],  # Embeded search value
    anns_field="embedding",  # Search across embeddings
    param=search_params,
    limit=100,  # Limit to five results per search
    output_fields=['appid', 'game_title', 'review', 'playtime_forever', 'review_date', 'resonance_score']  # Include title field in result
)
```

```python
type(test_results[0])
```
```
pymilvus.orm.search.Hits
```

```python
ret=[]
for hit in test_results[0]:
    row=[]
    row.extend([hit.id, 
                hit.score, 
                hit.entity.get('appid'),
                hit.entity.get('game_title'),
                hit.entity.get('review'),
                hit.entity.get('playtime_forever'),
                hit.entity.get('review_date'),
                hit.entity.get('resonance_score')])  # Get the id, distance, and title for the results
    ret.append(row)

ret_df = pd.DataFrame(ret, columns=['id', 'score', 'appid', 'game_title', 'review', 'playtime_forever', 'review_date', 'resonance_score'])
```

```python
ret_df
```
```
           id     score    appid       game_title   
0    85390437  0.260416   951440       Volcanoids  \
1    65727993  0.260780   951440       Volcanoids   
2   131452704  0.272909   951440       Volcanoids   
3    78509448  0.273827   951440       Volcanoids   
4   124543087  0.280990  1392650      BLASTRONAUT   
..        ...       ...      ...              ...   
95  114996887  0.336969  1768300       Grid Miner   
96   19898496  0.337283   413740    Mines of Mars   
97   48258980  0.337513   909570  Spuds Unearthed   
98  109218356  0.337515   844380   Cave Digger VR   
99  121115839  0.337531   844380   Cave Digger VR   

                                               review  playtime_forever   
0   Its a great game, but before you buy it - KNOW...              3328  \
1   I recommend if you're into a Dieselpunk aesthe...              1885   
2   Story: The story is not explained ingame very ...              1486   
3    Drillship taking damage! Lava I shall begin t...              9450   
4   Hours of space-prospecting, rock-blasting fun!...              2730   
..                                                ...               ...   
95  Grid Miner very cool strategy puzzle game good...                56   
96  This is wonderful! Like a modern Dig-Dug from ...              4956   
97  Somebody needs to get a Tutorial video made pr...               214   
98  first i want to talk about the good things the...                36   
99  The game itself can be repetitive, and it isn'...               348   

   review_date  resonance_score  
0   2021-01-26      1220.272217  
1   2020-03-24      1423.664795  
2   2023-01-24      1846.415283  
3   2020-11-01      1695.905273  
4   2022-10-28       775.358521  
..         ...              ...  
95  2022-05-05        91.140701  
96  2015-12-24       331.904297  
97  2019-01-11       334.284943  
98  2022-01-31        93.330215  
99  2022-08-23       182.267609  

[100 rows x 8 columns]
```

```python
ret_df.sort_values(by=['resonance_score'], ascending=False)
```
```
           id     score    appid                    game_title   
81   91904021  0.331788   951440                    Volcanoids  \
2   131452704  0.272909   951440                    Volcanoids   
3    78509448  0.273827   951440                    Volcanoids   
12   50454773  0.298559   951440                    Volcanoids   
77   31297857  0.331646   311910  DIG IT! - A Digger Simulator   
..        ...       ...      ...                           ...   
43  116111190  0.322824  1526380        Excavator Simulator VR   
27    9756432  0.314876   273820  Mining & Tunneling Simulator   
74   84931668  0.331436   755550         True Mining Simulator   
67   23426066  0.330537   447960                    XCavalypse   
63   20439288  0.329233   273820  Mining & Tunneling Simulator   

                                               review  playtime_forever   
81  This game doesn’t really deserve a thumbs down...              1178  \
2   Story: The story is not explained ingame very ...              1486   
3    Drillship taking damage! Lava I shall begin t...              9450   
12  As of this review I've played for about 8 hour...              1010   
77  I'm an experienced operator so I already know ...               907   
..                                                ...               ...   
43  If you enjoy heavy equipment, you will enjoy t...                47   
27  Phenomenal game, its got good storytelling and...                67   
74  A great idea to a type of game i have been sea...                49   
67  For a buck I've killed zombies with a big shov...                 6   
63  At first I enjoyed the game,Then it came to th...                19   

   review_date  resonance_score  weighted_score  
81  2021-05-12      1850.471069     5577.277387  
2   2023-01-24      1846.415283     6765.685224  
3   2020-11-01      1695.905273     6193.354769  
12  2019-05-03      1511.645386     5063.139743  
77  2017-04-22      1493.440063     4503.113807  
..         ...              ...             ...  
43  2022-05-27        59.633320      184.723713  
27  2014-04-07        57.987263      184.158787  
74  2021-01-18        31.485590       94.997587  
67  2016-06-06        22.966339       69.481959  
63  2016-01-10        22.124638       67.200476  

[100 rows x 9 columns]
```

```python
# Because the 'score' column is the distance from the query, we can weight the distance by the resonance score to get a better ranking
```

```python
ret_df[ret_df['appid'] == 252410]
```
```
           id     score   appid      game_title   
21  100946214  0.311362  252410  SteamWorld Dig  \
33   32956351  0.318485  252410  SteamWorld Dig   
51  120459114  0.326317  252410  SteamWorld Dig   
53   12113242  0.326965  252410  SteamWorld Dig   
71   47574679  0.330833  252410  SteamWorld Dig   

                                               review  playtime_forever   
21   All of its nuts and bolts in the right place ...               980  \
33  Well, that was fast. tl;dr; An easily forgetta...               490   
51  2D block crusher with addictive gameplay and p...              1548   
53  Put in a blender Terraria, Mr Driller, Metroid...              1168   
71  SteamWorld Dig is best described as a casual g...               458   

   review_date  resonance_score  weighted_score  
21  2021-10-12       844.897888     2713.556757  
33  2017-06-28       869.538574     2730.231256  
51  2022-08-11       843.168213     2583.893849  
53  2014-09-15       931.468811     2848.831021  
71  2018-12-16       913.736694     2761.930883  
```

```python
ret_df['weighted_score'] = ret_df['resonance_score'] / ret_df['score']
ret_df.sort_values(by=['weighted_score'], ascending=False)
```
```
           id     score    appid                    game_title   
2   131452704  0.272909   951440                    Volcanoids  \
3    78509448  0.273827   951440                    Volcanoids   
81   91904021  0.331788   951440                    Volcanoids   
1    65727993  0.260780   951440                    Volcanoids   
8   130448702  0.292804   951440                    Volcanoids   
..        ...       ...      ...                           ...   
43  116111190  0.322824  1526380        Excavator Simulator VR   
27    9756432  0.314876   273820  Mining & Tunneling Simulator   
74   84931668  0.331436   755550         True Mining Simulator   
67   23426066  0.330537   447960                    XCavalypse   
63   20439288  0.329233   273820  Mining & Tunneling Simulator   

                                               review  playtime_forever   
2   Story: The story is not explained ingame very ...              1486  \
3    Drillship taking damage! Lava I shall begin t...              9450   
81  This game doesn’t really deserve a thumbs down...              1178   
1   I recommend if you're into a Dieselpunk aesthe...              1885   
8   Volcanoids is by all means an interesting conc...               947   
..                                                ...               ...   
43  If you enjoy heavy equipment, you will enjoy t...                47   
27  Phenomenal game, its got good storytelling and...                67   
74  A great idea to a type of game i have been sea...                49   
67  For a buck I've killed zombies with a big shov...                 6   
63  At first I enjoyed the game,Then it came to th...                19   

   review_date  resonance_score  weighted_score  
2   2023-01-24      1846.415283     6765.685224  
3   2020-11-01      1695.905273     6193.354769  
81  2021-05-12      1850.471069     5577.277387  
1   2020-03-24      1423.664795     5459.260287  
8   2023-01-08      1488.157349     5082.437195  
..         ...              ...             ...  
43  2022-05-27        59.633320      184.723713  
27  2014-04-07        57.987263      184.158787  
74  2021-01-18        31.485590       94.997587  
67  2016-06-06        22.966339       69.481959  
63  2016-01-10        22.124638       67.200476  

[100 rows x 9 columns]
```

```python
# Collapse duplicate appids, averaging the 'score' and 'resonance_score' columns
ret_df.groupby('appid').mean(numeric_only=True).sort_values(by=['weighted_score'], ascending=[False])
```
```
                   id     score  playtime_forever  resonance_score   
appid                                                                
951440   1.037786e+08  0.295968       2171.153846      1457.624390  \
704510   3.582510e+07  0.331666      84297.000000      1124.269165   
577670   1.050246e+08  0.329354       1529.000000      1067.702271   
650350   3.413721e+07  0.331259       2712.000000      1007.544922   
65270    1.713078e+07  0.312437       2343.000000       882.987671   
200370   7.999551e+06  0.324779       1237.000000       910.839783   
1392650  1.245431e+08  0.280990       2730.000000       775.358521   
252410   6.280992e+07  0.322792        928.800000       880.562012   
513480   3.283160e+07  0.315348       1833.000000       749.715637   
321830   1.257264e+07  0.319768        878.500000       710.801025   
35480    2.801153e+07  0.329618       1236.333333       725.025818   
376250   2.523074e+07  0.326012       1437.000000       718.975464   
223430   1.151511e+07  0.336783        530.000000       699.428284   
507120   5.073322e+07  0.332280       1338.000000       576.899841   
593070   4.857250e+07  0.332408        657.250000       521.930176   
329310   4.160821e+07  0.313059       1200.000000       484.115662   
311910   3.929315e+07  0.318942        836.454545       479.815521   
567370   4.903708e+07  0.333333        518.500000       495.962036   
402750   7.280439e+07  0.330902        386.000000       475.553040   
211180   1.025627e+07  0.327072        776.000000       464.098877   
1441790  9.805202e+07  0.323630        572.666667       429.246063   
1523510  1.129983e+08  0.323182       1631.000000       370.950836   
1528050  9.732513e+07  0.326920        848.000000       360.220428   
503340   2.500221e+07  0.323202        238.500000       328.156830   
909570   4.825898e+07  0.337513        214.000000       334.284943   
413740   8.916373e+07  0.330978       2001.333333       293.118317   
844380   9.297633e+07  0.324057        368.250000       232.799789   
350620   2.543205e+07  0.329275        619.000000       226.023651   
1526380  1.015925e+08  0.317179        208.000000       169.865860   
273820   1.667847e+07  0.320194        108.400000       121.145363   
1768300  1.149969e+08  0.336969         56.000000        91.140701   
447960   2.597152e+07  0.322492        100.000000        83.277367   
755550   8.493167e+07  0.331436         49.000000        31.485590   

         weighted_score  
appid                    
951440      4953.843876  
704510      3389.761641  
577670      3241.804693  
650350      3041.564212  
65270       2826.126540  
200370      2804.494313  
1392650     2759.376463  
252410      2727.688753  
513480      2377.425183  
321830      2238.869901  
35480       2207.880971  
376250      2205.361700  
223430      2076.791444  
507120      1736.188155  
593070      1569.854004  
329310      1546.403899  
311910      1492.764885  
567370      1486.215233  
402750      1437.141197  
211180      1418.952154  
1441790     1329.982360  
1523510     1148.941229  
1528050     1101.859878  
503340      1023.441313  
909570       990.436979  
413740       884.839242  
844380       725.005043  
350620       689.800336  
1526380      537.121465  
273820       381.708368  
1768300      270.472051  
447960       263.943906  
755550        94.997587  
```

```python
# Find the counts of each appid in the resulting dataframe
ret_df['appid'].value_counts()
```
```
appid
951440     13
1526380    11
311910     11
844380      8
252410      5
273820      5
1523510     4
503340      4
593070      4
1441790     3
447960      3
35480       3
413740      3
321830      2
567370      2
350620      2
1768300     1
223430      1
402750      1
704510      1
755550      1
650350      1
507120      1
200370      1
577670      1
211180      1
1528050     1
376250      1
1392650     1
513480      1
329310      1
65270       1
909570      1
Name: count, dtype: int64
```

```python
# Plot a bar graph of counts for appids with greater than 2 value counts
ret_df['appid'].value_counts()[ret_df['appid'].value_counts() > 2].plot(kind='bar')
```
![[Pasted image 20240929215942.png]]

```python
for hit in test_results[0]:
    print(hit)
```
```
id: 85390437, distance: 0.2604157626628876, entity: {'playtime_forever': 3328, 'review_date': '2021-01-26', 'resonance_score': 1220.2722, 'appid': 951440, 'game_title': 'Volcanoids', 'review': "Its a great game, but before you buy it - KNOW what you're actually buying: this is an open world, slightly linear FPS - with elaborate base management. All the base management parts serve to equip you well for your fighting, and while there's tiny bit of node mining, its irrelevant and not necessary since you can get nearly everything from raiding enemy drills. You cannot, however, go forward JUST via mining - you have to fight. The base management is really elaborate and complex, and I love in just how many useful ways you get to tune your drill. I'm not even at a half, so I'm sure all these 'automation' points have even more meaning later on. FPS part is great, but at the same time, nothing you haven't seen before. Few different weapons, endless enemies, loot them for parts, re-craft amour, ammo, keep going. You can go anywhere on the island, but some parts are gated via upgrades you need for your drill. Since you can only move the drill more or less freely while underground, and emerge overground (or caves) only at predetermined spots - this adds some strategy. At any point in time, you can order your drill to go underground, remotely, and then emerge at the point closest to you, given that an enemy isn't there already. Thing is, if your drill goes underground, enemy can takeover nearby emerge points, in which case you now have to run to wherever you can, in order to emerge your drill - and some of these points are hundreds and hundreds of meters apart. While this would be just an annoyance normally - the eruptions keep on happening, and there's a countdown timer between each one. I haven't died to an eruption yet, so I don't really know what happens, but you have to be in your drill and dive underground, to be safe during the eruption. This timer on higher difficulties REALLY adds pressure and stress, and necessity for planning - which is awesome. I had this game on my wishlist for months and was always pushed away by bad reviews. I don't know what those reviews expect, but this game is definitely worth its asking price, and more, since updates keep on happening."}
id: 65727993, distance: 0.26077979803085327, entity: {'playtime_forever': 1885, 'review_date': '2020-03-24', 'resonance_score': 1423.6648, 'appid': 951440, 'game_title': 'Volcanoids', 'review': 'I recommend if you\'re into a Dieselpunk aesthetic with building. If you\'re looking for a good shooter, strategic combat or an interesting narrative, then this won\'t be for you. If you want a gorgeous ship you piece together, get shot up, rebuild, then rip apart yourself and layout from scratch given your current idea of optimal, then this is pretty good, not great, but good. First the negative. Bear with me. All the guns are bound to the same key, including a mortar that can kill you if you happen to switch to it pressing the 3 key while trying to get your shotgun. Said mortar is highly lethal to yourself point-blank, and shoot way off to the right, likely clipping the wall and killing you. Enemy bots, despite the clear colour coding and recognising what type they are, and noting the in archive that they have decent stats, function exactly the same. Except for thinking the blank soldier types take more of a beating, I can\'t tell you that they functionally hit you from different distances, miss more often, hurt less. In terms of gaming experience they feel the same. Same patterns you use to fight them. There\'s not much in the fighting them - headshots, if they do anything at all, don\'t seem to matter, and they simply blind-rush forward and fire as they approach. There\'s little sound to their approach, and they spawn from the cog spawners inside the ship you\'re boarding, frequently stacking inside one another as they run into your room. That and with the present build they can\'t fire through boulders, but they can move into them and fire out while you can\'t see nor shoot them and need to waste explosives to defeat them. Between the layout of the drillships and rocky terrain you\'ll frequently find "looks clear" turns into a bloodbath with little to be done from your side. You\'d think that taking the high ground would prevent such ambushes, but since your guns pretty much don\'t outrange them, you\'re just getting guaranteed hit by all of them. Combat is not a small part of it, and plays out very samy in most encounters regardless of terrain, ship type you\'re facing and your layout. Only running out of ammo really affects anything. As at time of writing, just after the Optimisation update, the game worked 98% without glitches, but the tutorial/guide still needs work. Some of it like needing a tier 1 module currently installed to be able to produce a tier 2 you\'ve just researched isn\'t quite as intuitive, but that should improve. Now, the positive - building is satisfying, mining isn\'t annoying. Your drillship starts feeling like home and like a beast. When you get the resources play around with all those gauges and crazy controllers you can install. There is some polish to this already, like losing your only production facility has the sub still parked in the bay where you can reconstruct that ability. If your ship took a beating driving through too thick a portion of lava or you neglected it too lightly turreted, you can cruise around underground collecting resources as the mining vessel you are, then find a secluded cavern to repair and retool. Other than the guns having barebones animation for shooting/reloading, the sounds of the ship itself and the visuals while it does what is does, including the internal corridors snaking as it descends underground are great. By extension, I\'m looking for Volcanoids in 2 years time, or Volcanoids 2. Where you get multiplayer running off the same ship you build together, get to interact with systems from the outside as well as inside and more varied enemy encounters, if combat remains as prominent.'}
...
id: 63197737, distance: 0.33590325713157654, entity: {'playtime_forever': 214, 'review_date': '2020-02-08', 'resonance_score': 149.52603, 'appid': 844380, 'game_title': 'Cave Digger VR', 'review': "I've been waiting to buy this game for too long, and now that its mine, I've barely scratched the surface and fell in love! Its a great casual vr game that can make you feel accomplished in a small run alone! It has its own interesting world and a variety of things to do. If you've been waiting to buy the game like me, stop waiting and pick it up!"}
id: 11515106, distance: 0.33678311109542847, entity: {'playtime_forever': 530, 'review_date': '2014-08-01', 'resonance_score': 699.4283, 'appid': 223430, 'game_title': 'Miner Wars 2081', 'review': 'When I first bought this game I was hoping I could step off my ship. so if you\'re looking for that it\'s not here! However, this game does bring a certain addiction with it. The gameplay is relatively simple, your ship is flown in a bigger ship that travels around a certain area of space which is controlled by different countries or factions. There is a storyline to follow and is a decent one at that. Like every game I play though I base its greatness largely on the game play. This game is about what you can expect from any first person space shooter game with a multitude of upgrades available (from armor, to weapons, to ammunition type, engines etc. ). Adding the ability to dig through rock or mine adds a different paradigm to the game (. sort of. You can mine and then sell the rocks/element types). There is a different drill though if you press "m" that just rips through any asteroid in your way. This is not only pretty cool to mess with physical features of the map, but launching a few missiles also changes the shape or hole size in the asteroid. Although I personally like playing it, for it to really stand out and be something else and differentiate itself from any other game I can\'t really say. It is definitely a fun game to play, and worth a buy if on sale. The amount of control you are able to have on how your ship moves/rotates/displaces itself is quite notable in this game. If you really like having control of your space craft, this is it! Beware though, some players may find this repetitive. RATING : 7.1/10 Gameplay B Story / Campaign B- Visuals / User Interface C+ Sounds / Music B- Replay-ability B- Overall B- '}
id: 114996887, distance: 0.3369690179824829, entity: {'playtime_forever': 56, 'review_date': '2022-05-05', 'resonance_score': 91.1407, 'appid': 1768300, 'game_title': 'Grid Miner', 'review': 'Grid Miner very cool strategy puzzle game good graphics and music good tutorial Unlock new tools and utilities as you progress through levels mine asteroids to gather resources to get and maintain your quotas unique buildings like power plants, asteroid crushers, tractor beams and more ,levels to test your skills and rack you brain hoping there will be more levels may be a steam workshop ?? so players can make some levels '}
id: 19898496, distance: 0.33728310465812683, entity: {'playtime_forever': 4956, 'review_date': '2015-12-24', 'resonance_score': 331.9043, 'appid': 413740, 'game_title': 'Mines of Mars', 'review': 'This is wonderful! Like a modern Dig-Dug from hell. :D'}
id: 48258980, distance: 0.3375125825405121, entity: {'playtime_forever': 214, 'review_date': '2019-01-11', 'resonance_score': 334.28494, 'appid': 909570, 'game_title': 'Spuds Unearthed', 'review': "Somebody needs to get a Tutorial video made pronto!! As other have said there's no instruction what so ever. The core of the game is self explanatory, you drop your little dudes on to the battlefield, you man the cannon to aid against advancing enemy, while providing vehicular support with tanks or planes. Away from that there's an update level where you can specialize your grunts, update the cannons to different types of fire, and create different plane/tank types (i think). Thats all done with hand motion interaction, you pull levers, grab coins from draws you pull out to place in slots, kind of like skyworld if you've played that. The third screen is where you seem to pick your levels to conquer, you grab a planet and use your two handles pushed forward to engage the selected level. The game is fun, but i would say it needs a leveling system on your grunts (if one doesn't exist) to keep your attention. i'll tentatively recommend it, there's lots to like from the core game to the wonderful graphics, the VR interactions and seemless gameplay elements. Just keep your eye on the clock while figuring the game out, you'll spend a while doing that. "}
id: 109218356, distance: 0.3375145196914673, entity: {'playtime_forever': 36, 'review_date': '2022-01-31', 'resonance_score': 93.330215, 'appid': 844380, 'game_title': 'Cave Digger VR', 'review': "first i want to talk about the good things the mining is not bad 70% of the time and the game looks good now to the bad shit 1. The every thing, the music is annoying some time your pickax just goes through the rock with out breaking it, the fact you can grab things without binding down is great IF IT WORKED its too slow half the time it dosnt pick up the things you want so just binding down is faster and more efficient, Undead Development did this perfectly by you only need to hover your hand over it it teleport-ed in your hand making it fast and easier on your back sometimes you get stuck to things for no reason and also items get stuck to things for no reason if you put anything in the holster you can't grab things off the ground cause you'll grab the item in your holster the fact you have to smash the glass every time you boot up the game is annoying making your first mine slower overall 2/10 don't get find another mining game cause this gem is worth nothing."}
id: 121115839, distance: 0.3375311493873596, entity: {'playtime_forever': 348, 'review_date': '2022-08-23', 'resonance_score': 182.26761, 'appid': 844380, 'game_title': 'Cave Digger VR', 'review': "The game itself can be repetitive, and it isn't extremely apparent that you can buy a train to expand the game world, and mine in new areas. Beyond its issues, its still decently fun, and I would personally recommend it if you're looking for a new vr game."}

```

```python
review_collection.num_entities
```
```
360840
```
